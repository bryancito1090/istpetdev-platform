#!/usr/bin/env python3
"""Backup, isolated restore rehearsal and migration; requires an active TLS tunnel."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import time

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'ops'))
import dev


def pg_tool(tool, credential, config, ca, *, output=None, source=None, extra=(), database=None):
    # Exact server major, independent of the host's pg_dump version. No password in env/argv.
    with dev.pg_env(credential, config, ca, database) as env:
        cmd = ['docker', 'run', '--rm', '--network', 'host', '-i',
               '-v', env['PGPASSFILE'] + ':/run/pgpass:ro', '-v', ca + ':/run/rds-ca.pem:ro']
        for key in ['PGHOST', 'PGHOSTADDR', 'PGPORT', 'PGDATABASE', 'PGUSER', 'PGSSLMODE', 'PGCONNECT_TIMEOUT']:
            cmd += ['-e', key + '=' + env[key]]
        cmd += ['-e', 'PGPASSFILE=/run/pgpass', '-e', 'PGSSLROOTCERT=/run/rds-ca.pem', 'postgres:16-alpine', tool, *extra]
        result = subprocess.run(cmd, stdin=source, stdout=output or subprocess.PIPE, stderr=subprocess.PIPE)
        if result.returncode:
            raise RuntimeError(f'{tool} failed; no migration or restore accepted')
        return result.stdout


def fingerprint(credential, config, ca, db=None):
    tables = dev.sql(credential, config, ca, "SELECT tablename FROM pg_tables WHERE schemaname='public' ORDER BY tablename;", db).splitlines()
    result = {}
    for table in tables:
        identifier = '"' + table.replace('"', '""') + '"'
        result[table] = dev.sql(credential, config, ca, f"SELECT count(*) || ':' || md5(coalesce(string_agg(row_to_json(t)::text, ',' ORDER BY row_to_json(t)::text), '')) FROM public.{identifier} t;", db)
    return result


def backup(credential, config, ca, directory, aws=None, bucket=None):
    path = dev.private_path(Path(directory) / (credential['dbname'] + '-' + time.strftime('%Y%m%dT%H%M%SZ', time.gmtime()) + '-' + secrets.token_hex(3) + '.dump'))
    before = fingerprint(credential, config, ca)
    with path.open('wb') as out:
        pg_tool('pg_dump', credential, config, ca, output=out, extra=['-Fc', '--no-owner', '--no-acl'])
    if path.stat().st_size == 0:
        raise RuntimeError('Empty backup')
    with path.open('rb') as source:
        pg_tool('pg_restore', credential, config, ca, source=source, extra=['--list'])
    after = fingerprint(credential, config, ca)
    if before != after:
        raise RuntimeError('Writes occurred during backup rehearsal; coordinate a quiet window and retry')
    digest = hashlib.sha256(path.read_bytes()).digest()
    manifest = {'database': credential['dbname'], 'sha256': digest.hex(), 'tables': before, 'size': path.stat().st_size}
    if aws:
        key = 'daily/' + path.name
        checksum = base64.b64encode(digest).decode('ascii')
        result = aws.call('s3api', 'put-object', bucket=bucket, key=key, body=str(path), checksum_algorithm='SHA256', checksum_sha256=checksum, server_side_encryption='AES256')
        head = aws.call('s3api', 'head-object', bucket=bucket, key=key, version_id=result['VersionId'], checksum_mode='ENABLED')
        if head['ContentLength'] != path.stat().st_size or head['ChecksumSHA256'] != checksum:
            raise RuntimeError('S3 backup integrity verification failed')
        manifest['s3'] = {'bucket': bucket, 'key': key, 'version': result['VersionId']}
        if time.gmtime().tm_wday == 6:
            aws.call('s3api', 'put-object', bucket=bucket, key='weekly/' + path.name, body=str(path), checksum_algorithm='SHA256', checksum_sha256=checksum, server_side_encryption='AES256')
    dev.write_private(str(path) + '.json', json.dumps(manifest, indent=2))
    return path


def restore_check(path, master, config, ca):
    path = Path(path)
    manifest = json.loads(Path(str(path)+'.json').read_text())
    if hashlib.sha256(path.read_bytes()).hexdigest() != manifest['sha256']:
        raise RuntimeError('Backup checksum mismatch')
    name = 'istpetdev_restorecheck_' + secrets.token_hex(6)
    started = time.monotonic()
    dev.sql(master, config, ca, f'CREATE DATABASE {name};')
    try:
        dev.sql(master, config, ca, f'REVOKE ALL ON DATABASE {name} FROM PUBLIC;')
        with path.open('rb') as source:
            pg_tool('pg_restore', master, config, ca, source=source, database=name,
                    extra=['--exit-on-error', '--no-owner', '--no-acl', '--dbname', name])
        if fingerprint(master, config, ca, name) != manifest['tables']:
            raise RuntimeError('Restored data differs from the backup manifest')
        version = dev.sql(master, config, ca, 'SELECT postgis_version();', name)
        report = {'restored': True, 'seconds': round(time.monotonic()-started, 2), 'postgis': version,
                  'tables_verified': len(manifest['tables']), 'roles': 'Reapply bootstrap grants before any promoted recovery; rehearsal is admin-only'}
        dev.write_private(str(path)+'.restore.json', json.dumps(report, indent=2))
    finally:
        # Only the random database created by this invocation is removed.
        dev.sql(master, config, ca, f'DROP DATABASE {name};')
    return report


def migrate(credential, config, ca):
    # This task never downgrades or resets a database. Integration migrations are serialized.
    statement = dev.run(['dotnet', 'ef', 'migrations', 'script', '--idempotent', '--no-build',
                         '--project', str(dev.ROOT/'src/Infrastructure'), '--startup-project', str(dev.ROOT/'src/Api')])
    dev.sql(credential, config, ca, 'SELECT pg_advisory_lock(742901);\n' + statement + '\nSELECT pg_advisory_unlock(742901);')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=['backup', 'restore-check', 'migrate'])
    parser.add_argument('--profile', required=True)
    parser.add_argument('--config', required=True)
    parser.add_argument('--outputs', required=True)
    parser.add_argument('--member', choices=dev.MEMBERS + ['integration'], default='integration')
    parser.add_argument('--directory', default=str(dev.PRIVATE/'backups'))
    parser.add_argument('--backup', help='Existing private dump for restore-check')
    args = parser.parse_args()
    config = json.loads(Path(args.config).read_text())
    if config['environment'] != 'dev' or config['aws_region'] != 'us-east-1':
        parser.error('Only dev/us-east-1 is supported')
    outputs = json.loads(Path(args.outputs).read_text())
    aws, ca = dev.Aws(args.profile), dev.ca_file()
    key = 'integration/backup' if args.member == 'integration' else args.member+'/migrator'
    credential = json.loads(aws.call('secretsmanager', 'get-secret-value', secret_id='/istpetdev/dev/db/'+key)['SecretString'])
    dev.checked_secret(credential, config['rds_address'], 'istpetdev_'+args.member,
                       'backup_integration' if args.member == 'integration' else 'migrator_'+args.member)
    if args.operation == 'restore-check':
        if not args.backup:
            parser.error('--backup is required')
        master = json.loads(aws.call('secretsmanager', 'get-secret-value', secret_id=outputs['rds_master_user_secret_arn']['value'])['SecretString'])
        master['dbname'] = 'postgres'
        report = restore_check(args.backup, master, config, ca)
        print(f'Restored and verified {report["tables_verified"]} tables in {report["seconds"]} seconds; temporary database removed.')
        return
    path = backup(credential, config, ca, args.directory, aws, outputs['backup_bucket_name']['value'])
    if args.operation == 'migrate':
        migrator_key = 'integration/migrator' if args.member == 'integration' else args.member+'/migrator'
        migrator = json.loads(aws.call('secretsmanager', 'get-secret-value', secret_id='/istpetdev/dev/db/'+migrator_key)['SecretString'])
        dev.checked_secret(migrator, config['rds_address'], 'istpetdev_'+args.member, 'migrator_'+args.member)
        migrate(migrator, config, ca)
        print('Idempotent migrations applied; backup verified in S3 before DDL.')
    print('Private backup: ' + str(path))


if __name__ == '__main__':
    try:
        main()
    except (ValueError, RuntimeError, OSError, KeyError) as e:
        print(f'Database task stopped ({type(e).__name__}); no success assumed.', file=sys.stderr)
        sys.exit(1)
