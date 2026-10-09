#!/usr/bin/env python3
"""Dev operations. AWS CLI + libpq; secrets travel via stdin/private files only."""
import argparse
import contextlib
import json
import os
import re
from pathlib import Path
import secrets
import shlex
import socket
import ssl
import subprocess
import sys
import tempfile
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
PRIVATE = Path.home() / '.config/istpetdev/private'
MEMBERS = [f'dev_{i:02}' for i in range(1, 6)]
os.umask(0o077)


def private_path(path):
    path = Path(path).expanduser().resolve()
    if path.is_relative_to(ROOT) or path.is_relative_to(ROOT.parent / 'HACKATHON'):
        raise ValueError('Private artifacts must be outside both repositories')
    path.parent.mkdir(parents=True, exist_ok=True, mode=0o700)
    return path


def write_private(path, content):
    path = private_path(path)
    with tempfile.NamedTemporaryFile(mode='w', dir=path.parent, delete=False) as f:
        f.write(content)
    os.replace(f.name, path)
    return path


def run(argv, *, data=None, env=None):
    result = subprocess.run(argv, input=data, text=True, capture_output=True, env=env)
    if result.returncode:
        # stderr can echo SQL / credentials; never copy it into logs or exceptions.
        raise RuntimeError(f'{Path(argv[0]).name} failed (exit {result.returncode}); values and SQL withheld')
    return result.stdout


class AwsError(RuntimeError):
    def __init__(self, service, operation, code):
        self.code = code
        super().__init__(f'AWS {service}:{operation}: {code}')


class Aws:
    def __init__(self, profile, region='us-east-1'):
        self.env = {**os.environ, 'AWS_PROFILE': profile, 'AWS_REGION': region,
                    'AWS_DEFAULT_REGION': region, 'AWS_PAGER': ''}

    def call(self, service, operation, *, payload=None, missing=False, **args):
        cmd = ['aws', service, operation, '--no-cli-pager', '--output', 'json']
        for key, value in args.items():
            cmd += ['--' + key.replace('_', '-'), str(value)]
        with contextlib.ExitStack() as stack:
            if payload is not None:
                f = stack.enter_context(tempfile.NamedTemporaryFile(mode='w+', encoding='utf-8'))
                json.dump(payload, f)
                f.flush()
                cmd += ['--cli-input-json', 'file://' + f.name]
            result = subprocess.run(cmd, capture_output=True, text=True, env=self.env)
            if result.returncode:
                if missing and '(ResourceNotFoundException)' in result.stderr:
                    return None
                error = re.search(r'An error occurred \(([A-Za-z0-9]+)\)', result.stderr)
                raise AwsError(service, operation, error[1] if error else 'CLIError')
            return json.loads(result.stdout or '{}')


def ca_file(path=None):
    path = Path(path or Path.home() / '.config/istpetdev/certs/global-bundle.pem')
    if not path.exists():
        if path.name != 'global-bundle.pem':
            raise ValueError('Specified CA does not exist')
        with urllib.request.urlopen('https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem', timeout=30) as r:
            content = r.read().decode('ascii')
        ssl.create_default_context(cadata=content)
        write_private(path, content)
    ssl.create_default_context(cafile=str(path))
    return str(path.resolve())


def checked_secret(value, host, dbname, username):
    if not isinstance(value, dict) or any(value.get(k) != v for k, v in
        {'host': host, 'dbname': dbname, 'username': username}.items()):
        raise ValueError('Secret metadata differs from the expected identity/database')
    if not isinstance(value.get('password'), str) or not value['password'] or any(c in value['password'] for c in '\r\n\0'):
        raise ValueError('Invalid password in secret')
    return value


@contextlib.contextmanager
def pg_env(secret, config, ca, dbname=None):
    # hostaddr routes through the tunnel, host verifies the real certificate name.
    def escape(v):
        return str(v).replace('\\', '\\\\').replace(':', '\\:')
    with tempfile.NamedTemporaryFile(mode='w+') as f:
        f.write(':'.join(escape(v) for v in [config['rds_address'], config['local_tunnel_port'], '*', secret['username'], secret['password']]) + '\n')
        f.flush()
        env = {k: v for k, v in os.environ.items() if not k.startswith('PG')}
        env.update(PGHOST=config['rds_address'], PGHOSTADDR='127.0.0.1', PGPORT=str(config['local_tunnel_port']),
                   PGDATABASE=dbname or secret['dbname'], PGUSER=secret['username'], PGPASSFILE=f.name,
                   PGSSLMODE='verify-full', PGSSLROOTCERT=ca, PGCONNECT_TIMEOUT='10')
        yield env


def sql(secret, config, ca, statement, dbname=None):
    with pg_env(secret, config, ca, dbname) as env:
        return run(['psql', '-X', '-qAt', '-v', 'ON_ERROR_STOP=1'], data=statement, env=env).strip()


def literal(value):
    return "'" + value.replace("'", "''") + "'"


def bootstrap(args, config):
    ca = ca_file(args.ca)
    local = Path(args.local_secrets) if args.local_secrets else None
    aws = None if local else Aws(args.profile)
    if local:
        # Only disposable TLS databases with the same verification as AWS.
        master = json.loads(Path(args.local_master).read_text())
        store = json.loads(local.read_text()) if local.exists() else {}
    else:
        outputs = json.loads(Path(args.outputs).read_text())
        master = json.loads(aws.call('secretsmanager', 'get-secret-value', secret_id=outputs['rds_master_user_secret_arn']['value'])['SecretString'])
        store = {}
    master['dbname'] = 'postgres'
    sql(master, config, ca, 'SELECT 1;')
    records = []

    def credential(key, db, user):
        name = '/istpetdev/dev/db/' + key
        existing = store.get(key) if local else aws.call('secretsmanager', 'get-secret-value', secret_id=name, missing=True)
        if existing:
            result = existing if local else json.loads(existing['SecretString'])
            return checked_secret(result, config['rds_address'], db, user)
        if sql(master, config, ca, f'SELECT 1 FROM pg_roles WHERE rolname={literal(user)};') == '1':
            raise RuntimeError('Role exists without its secret; recover the secret before retrying')
        value = dict(host=config['rds_address'], port=5432, dbname=db, username=user, password=secrets.token_urlsafe(32))
        if local:
            store[key] = value
            write_private(local, json.dumps(store))
        else:
            # Secret containers are managed by Terraform. Missing container fails closed.
            aws.call('secretsmanager', 'put-secret-value', payload={'SecretId': name, 'SecretString': json.dumps(value)})
        return value

    for member in MEMBERS + ['integration']:
        db = 'istpetdev_' + member
        migrator = 'migrator_' + member
        migrator_key = 'integration/migrator' if member == 'integration' else member + '/migrator'
        people = MEMBERS if member == 'integration' else [member]
        rows = [(migrator_key, migrator)] + [
            (('integration/' + m if member == 'integration' else m), ('user_integ_' + m if member == 'integration' else 'user_' + m)) for m in people]
        if member == 'integration':
            rows.append(('integration/backup', 'backup_integration'))
        creds = [credential(key, db, user) for key, user in rows]
        if sql(master, config, ca, f'SELECT 1 FROM pg_database WHERE datname={literal(db)};') != '1':
            sql(master, config, ca, f'CREATE DATABASE {db};')
        sql(master, config, ca, f'REVOKE ALL ON DATABASE {db} FROM PUBLIC;')
        for c in creds:
            user = c['username']
            if sql(master, config, ca, f'SELECT 1 FROM pg_roles WHERE rolname={literal(user)};') != '1':
                sql(master, config, ca, f'CREATE ROLE {user} LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS PASSWORD {literal(c["password"])};')
            sql(master, config, ca, f'GRANT CONNECT ON DATABASE {db} TO {user};')
        # The admin retains ownership of the database and PostGIS; application has no DDL.
        sql(master, config, ca, f'CREATE EXTENSION IF NOT EXISTS postgis; REVOKE ALL ON SCHEMA public FROM PUBLIC; GRANT USAGE, CREATE ON SCHEMA public TO {migrator};', db)
        for c in creds[1:]:
            user = c['username']
            dml = 'SELECT' if user == 'backup_integration' else 'SELECT, INSERT, UPDATE, DELETE'
            sql(master, config, ca, f'''
GRANT USAGE ON SCHEMA public TO {user};
GRANT {dml} ON ALL TABLES IN SCHEMA public TO {user};
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO {user};
GRANT {migrator} TO {master['username']};
ALTER DEFAULT PRIVILEGES FOR ROLE {migrator} IN SCHEMA public GRANT {dml} ON TABLES TO {user};
ALTER DEFAULT PRIVILEGES FOR ROLE {migrator} IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO {user};
ALTER DEFAULT PRIVILEGES FOR ROLE {migrator} IN SCHEMA public REVOKE EXECUTE ON FUNCTIONS FROM PUBLIC;
REVOKE {migrator} FROM {master['username']};
''', db)
        for c in creds:
            sql(c, config, ca, 'SELECT 1;')  # Existing secret/role mismatch is never silently rotated.
            records.append(c)
        print(f'OK: {db}, PostGIS, roles and credentials verified')
    # PostgreSQL's maintenance databases must not bypass the personal isolation boundary.
    for db in ['postgres', 'template1']:
        sql(master, config, ca, f'REVOKE CONNECT, TEMPORARY ON DATABASE {db} FROM PUBLIC;')
    print('Bootstrap complete; existing data and passwords preserved.')


def npgsql(secret, config, ca):
    def quote(v):
        return '"' + str(v).replace('"', '""') + '"'
    values = dict(Host=config['rds_address'], Port=config['local_tunnel_port'], Database=secret['dbname'], Username=secret['username'], Password=secret['password'])
    values.update({'SSL Mode': 'VerifyFull', 'Root Certificate': ca, 'Maximum Pool Size': 10, 'Timeout': 10})
    return ';'.join(f'{k}={quote(v)}' for k, v in values.items()) + ';'


def load(args, config):
    ca = ca_file(args.ca)
    if {a[4][0] for a in socket.getaddrinfo(config['rds_address'], None, socket.AF_INET)} != {'127.0.0.1'}:
        raise ValueError('Map the real RDS hostname to 127.0.0.1 in /etc/hosts before loading the API connection')
    key = args.member if args.database == 'personal' else 'integration/' + args.member
    db = 'istpetdev_' + (args.member if args.database == 'personal' else 'integration')
    user = 'user_' + (args.member if args.database == 'personal' else 'integ_' + args.member)
    if args.migrator:
        if args.database == 'integration':
            key, user = 'integration/migrator', 'migrator_integration'
        else:
            key, user = args.member + '/migrator', 'migrator_' + args.member
    secret = json.loads(Aws(args.profile).call('secretsmanager', 'get-secret-value', secret_id='/istpetdev/dev/db/' + key)['SecretString'])
    checked_secret(secret, config['rds_address'], db, user)
    sql(secret, config, ca, 'SELECT 1;')
    values = {'ConnectionStrings:Main': npgsql(secret, config, ca)}
    if config.get('cognito'):
        values['Authentication:Authority'] = config['cognito']['issuer']
        for i, client in enumerate(config['cognito']['client_ids'].values()):
            values[f'Authentication:AllowedClientIds:{i}'] = client
    if args.mode == 'user-secrets':
        run(['dotnet', 'user-secrets', 'set', '--project', str(ROOT / 'src/Api')], data=json.dumps(values))
    else:
        lines = ['export ASPNETCORE_ENVIRONMENT=Development']
        lines += [f'export {k.replace(":", "__")}={shlex.quote(v)}' for k, v in values.items()]
        write_private(PRIVATE / 'dev-env.sh', '\n'.join(lines) + '\n')
    print('Connection verified with TLS; private configuration loaded. Run npm run dev:api.')


def export(args):
    outputs = json.loads(Path(args.outputs).read_text())
    keys = ['vpc_id', 'ec2_access_node_id', 'rds_instance_id', 'rds_address', 'rds_port', 'evidence_bucket_name', 'ssm_tunnel_document', 'cognito']
    config = {k: outputs[k]['value'] for k in keys}
    config.update(environment='dev', aws_region='us-east-1', local_tunnel_port=15432)
    path = write_private(Path(args.output_dir) / 'dev-access.json', json.dumps(config, indent=2))
    print(f'Access metadata written: {path}')


def tunnel(args, config):
    ca_file(args.ca)
    port = args.local_port or config['local_tunnel_port']
    if not 1024 <= int(port) <= 65535:
        raise ValueError('Local port must be 1024..65535')
    aws = Aws(args.profile)
    cmd = ['aws', 'ssm', 'start-session', '--no-cli-pager', '--target', config['ec2_access_node_id'],
           '--document-name', config['ssm_tunnel_document'], '--parameters', json.dumps({'localPortNumber': [str(port)]})]
    print('Npgsql requires the real RDS hostname mapped to 127.0.0.1; psql uses hostaddr.', flush=True)
    os.execvpe('aws', cmd, aws.env)


def render(args):
    outputs = json.loads(Path(args.outputs).read_text())
    for member, policy in outputs['developer_policies']['value'].items():
        write_private(Path(args.output_dir) / (member + '.json'), json.dumps(policy, indent=2))
    print('Rendered the exact policies already managed by Terraform; no wildcard secrets.')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    for name in ['bootstrap', 'load', 'tunnel']:
        p = sub.add_parser(name)
        p.add_argument('--profile', required=True)
        p.add_argument('--config', required=True)
        p.add_argument('--ca', help='PEM trust bundle; official RDS bundle downloaded by default')
        if name == 'bootstrap':
            p.add_argument('--outputs')
            p.add_argument('--local-secrets', help='Private persistent secret store for disposable TLS tests only')
            p.add_argument('--local-master', help='Private JSON master credential for disposable TLS tests only')
        elif name == 'load':
            p.add_argument('--member', choices=MEMBERS, required=True)
            p.add_argument('--database', choices=['personal', 'integration'], required=True)
            p.add_argument('--mode', choices=['user-secrets', 'file'], default='user-secrets')
            p.add_argument('--migrator', action='store_true', help='Load DDL identity for a single migration operation')
        else:
            p.add_argument('--local-port', type=int)
    for name in ['export', 'render']:
        p = sub.add_parser(name)
        p.add_argument('--outputs', required=True)
        p.add_argument('--output-dir', required=True)
    args = parser.parse_args()
    if args.command in ['export', 'render']:
        return globals()[args.command](args)
    config = json.loads(Path(args.config).read_text())
    if config.get('environment') != 'dev' or config.get('aws_region') != 'us-east-1':
        raise ValueError('Expected the dev environment in us-east-1')
    if args.command == 'bootstrap' and not args.outputs and not (args.local_secrets and args.local_master):
        parser.error('bootstrap needs --outputs or both local test files')
    return {'bootstrap': bootstrap, 'load': load, 'tunnel': tunnel}[args.command](args, config)


if __name__ == '__main__':
    try:
        main()
    except (ValueError, RuntimeError, OSError, KeyError) as e:
        # Do not print arbitrary parse errors; malformed secret values can appear in them.
        print(f'Operation stopped ({type(e).__name__}). Check login, permissions, metadata and TLS; no fallback applied.', file=sys.stderr)
        sys.exit(1)
