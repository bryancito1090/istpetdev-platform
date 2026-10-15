#!/usr/bin/env python3
"""Real disposable PostgreSQL/PostGIS test: TLS, rerun, grants and isolation."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tempfile
import time
import socket
import urllib.request
import urllib.error

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'ops'))
import dev
import importlib.util

# Una sola imagen para servidor, cliente pg_dump/pg_restore y el chown de los certificados: antes se
# descargaban tres (postgis, postgres:16-alpine y alpine) en cada ejecución de CI.
SERVER_IMAGE = 'postgis/postgis:16-3.4-alpine'
os.environ.setdefault('ISTPETDEV_PG_CLIENT_IMAGE', SERVER_IMAGE)
spec = importlib.util.spec_from_file_location("dbtask", Path(__file__).with_name("database-task.py"))
dbtask = importlib.util.module_from_spec(spec)
spec.loader.exec_module(dbtask)


def main():
    os.umask(0o077)
    name = 'istpetdev-bootstrap-test-' + secrets.token_hex(4)
    with tempfile.TemporaryDirectory(prefix='istpetdev-db-test-') as directory:
        path = Path(directory)
        master = {'username': 'postgres', 'password': secrets.token_urlsafe(32), 'dbname': 'postgres'}
        (path / 'password').write_text(master['password'])
        (path / 'password').chmod(0o444)  # Host parent remains 0700; container mounts just this file.
        (path / 'master.json').write_text(json.dumps(master))
        dev.run(['openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '1', '-subj', '/CN=localhost', '-addext', 'subjectAltName=DNS:localhost', '-keyout', str(path/'server.key'), '-out', str(path/'server.crt')])
        # Alpine postgres UID; only these newly generated ephemeral certificate files change owner.
        dev.run(['docker', 'run', '--rm', '-v', f'{path}:/test', SERVER_IMAGE, 'sh', '-c', 'chown 70:70 /test/server.key /test/server.crt; chmod 600 /test/server.key; chmod 644 /test/server.crt'])
        try:
            dev.run(['docker', 'run', '-d', '--name', name, '-p', '127.0.0.1::5432', '-v', f'{path}/password:/test/password:ro', '-v', f'{path}/server.key:/test/server.key:ro', '-v', f'{path}/server.crt:/test/server.crt:ro', '-e', 'POSTGRES_PASSWORD_FILE=/test/password', SERVER_IMAGE, 'postgres', '-c', 'ssl=on', '-c', 'ssl_cert_file=/test/server.crt', '-c', 'ssl_key_file=/test/server.key'])
            port = int(dev.run(['docker', 'port', name, '5432/tcp']).strip().rsplit(':', 1)[1])
            config = {'environment': 'dev', 'aws_region': 'us-east-1', 'rds_address': 'localhost', 'local_tunnel_port': port}
            args = argparse.Namespace(ca=str(path/'server.crt'), local_secrets=str(path/'secrets.json'), local_master=str(path/'master.json'), profile='unused', outputs=None)
            for _ in range(60):
                try:
                    dev.sql(master, config, args.ca, 'SELECT 1;')
                    break
                except RuntimeError:
                    time.sleep(0.5)
            else:
                with dev.pg_env(master, config, args.ca) as env:
                    result = subprocess.run(['psql', '-X', '-qAt', '-c', 'SELECT 1;'], env=env, capture_output=True, text=True)
                raise RuntimeError('Disposable PostgreSQL did not start: ' + result.stderr.replace(master['password'], '<redacted>'))
            dev.bootstrap(args, config)
            before = (path/'secrets.json').read_bytes()
            dev.bootstrap(args, config)
            assert before == (path/'secrets.json').read_bytes(), 'Bootstrap rotated credentials'
            store = json.loads(before)
            migrator, app = store['dev_01/migrator'], store['dev_01']
            dev.sql(migrator, config, args.ca, 'CREATE TABLE public.isolation_probe(id integer PRIMARY KEY);')
            dev.sql(app, config, args.ca, 'INSERT INTO public.isolation_probe VALUES(7);')
            assert dev.sql(app, config, args.ca, 'SELECT id FROM public.isolation_probe;') == '7'
            negatives = [(app, 'CREATE TABLE public.forbidden(id integer);', None),
                         (app, 'SELECT 1;', 'istpetdev_dev_02'),
                         (store['dev_02/migrator'], 'SELECT 1;', 'istpetdev_dev_01'),
                         (app, 'SELECT 1;', 'postgres'),
                         (store['integration/dev_01'], 'CREATE TABLE public.forbidden(id integer);', None)]
            for credential, query, db in negatives:
                try:
                    dev.sql(credential, config, args.ca, query, db)
                except RuntimeError:
                    pass
                else:
                    raise AssertionError('Isolation/DDL negative check unexpectedly succeeded')
            try:
                dev.sql(app, {**config, 'rds_address': 'wrong.invalid'}, args.ca, 'SELECT 1;')
            except RuntimeError:
                pass
            else:
                raise AssertionError('Wrong TLS hostname was accepted')
            assert dev.sql(app, config, args.ca, 'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid();') == 't'
            # /health/ready requires exactly the DemoCore tables (DatabaseHealthCheck), so the probe goes.
            dev.sql(migrator, config, args.ca, 'DROP TABLE public.isolation_probe;')
            dbtask.migrate(migrator, config, args.ca)
            dbtask.migrate(migrator, config, args.ca)
            # Every migration in the repository is applied exactly once, even after a second run.
            expected = sorted(p.stem for p in (dev.ROOT/'src/Infrastructure/Persistence/Migrations').glob('*.cs')
                              if p.stem[:14].isdigit() and not p.stem.endswith('.Designer'))
            applied = dev.sql(app, config, args.ca, 'SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY 1;').splitlines()
            assert applied == expected, f'Applied migrations {applied} differ from repository {expected}'
            print(f'PASS: {len(applied)} EF migrations applied once through API --migrate, run twice')
            with socket.socket() as sock:
                sock.bind(('127.0.0.1', 0))
                api_port = sock.getsockname()[1]
            api_env = {**os.environ, 'ASPNETCORE_ENVIRONMENT': 'Development',
                       'ConnectionStrings__Main': dev.npgsql(app, config, args.ca),
                       'Authentication__Authority': ''}
            with (path/'api.log').open('w+') as log:
                api = subprocess.Popen(['dotnet', str(dev.ROOT/'src/Api/bin/Debug/net8.0/IstpetDev.Api.dll'), '--urls', f'http://127.0.0.1:{api_port}'], env=api_env, stdout=log, stderr=log, cwd=dev.ROOT/'src/Api')
                try:
                    for _ in range(60):
                        try:
                            with urllib.request.urlopen(f'http://127.0.0.1:{api_port}/health/ready', timeout=1) as response:
                                assert response.status == 200
                            break
                        except (OSError, urllib.error.HTTPError):
                            time.sleep(0.25)
                    else:
                        raise AssertionError('API readiness failed after migration')
                    try:
                        urllib.request.urlopen(f'http://127.0.0.1:{api_port}/api/v1/service-points')
                    except urllib.error.HTTPError as e:
                        assert e.code == 401
                    else:
                        raise AssertionError('Unauthenticated business endpoint accepted')
                finally:
                    api.terminate()
                    api.wait(timeout=15)
                log.seek(0)
                content = log.read()
                assert not any(c['password'] in content for c in store.values()), 'Secret leaked in API log'
            print('PASS: local .NET API readiness over VerifyFull and protected endpoint 401')
            dump = dbtask.backup(migrator, config, args.ca, path/'backups')
            report = dbtask.restore_check(dump, master, config, args.ca)
            assert report['restored']
            print('PASS: EF migrations twice, dump with PostgreSQL 16, isolated restore and table hashes')
            # Ensure quoting cannot inject into .NET or a shell launcher.
            hostile = {**app, 'password': 'x;"$(touch /tmp/forbidden)'}
            connection = dev.npgsql(hostile, config, args.ca)
            assert 'Password="x;""$(touch /tmp/forbidden)";' in connection
            print('PASS: TLS/hostname, bootstrap twice without rotation, DML/DDL and cross-database isolation')
        finally:
            subprocess.run(['docker', 'rm', '-f', '-v', name], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


if __name__ == '__main__':
    main()
