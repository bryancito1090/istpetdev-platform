#!/usr/bin/env python3
"""Run the real EF/Npgsql/filter/controller in disposable PostgreSQL; never starts the API."""
import hashlib
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tempfile
import time

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'ops'))
import dev


def main():
    os.umask(0o077)
    name = 'istpetdev-schema95-runtime-' + secrets.token_hex(5)
    report = {'image': 'postgis/postgis:16-3.4-alpine', 'api_started': False, 'rds_tested': False}
    with tempfile.TemporaryDirectory(prefix='istpetdev-schema95-runtime-') as directory:
        path = Path(directory)
        master = {'username': 'postgres', 'password': secrets.token_urlsafe(32), 'dbname': 'postgres'}
        (path / 'password').write_text(master['password'])
        (path / 'password').chmod(0o444)  # Parent is 0700; only this file is mounted.
        dev.run(['openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-nodes', '-days', '1', '-subj', '/CN=localhost', '-addext', 'subjectAltName=DNS:localhost', '-keyout', str(path / 'server.key'), '-out', str(path / 'server.crt')])
        dev.run(['docker', 'run', '--rm', '--network', 'none', '-v', f'{path}:/test', 'alpine:3.20', 'sh', '-c', 'chown 70:70 /test/server.key /test/server.crt; chmod 600 /test/server.key; chmod 644 /test/server.crt'])
        try:
            dev.run(['docker', 'run', '-d', '--name', name, '--tmpfs', '/var/lib/postgresql/data', '-p', '127.0.0.1::5432', '-v', f'{path}/password:/test/password:ro', '-v', f'{path}/server.key:/test/server.key:ro', '-v', f'{path}/server.crt:/test/server.crt:ro', '-e', 'POSTGRES_PASSWORD_FILE=/test/password', report['image'], 'postgres', '-c', 'ssl=on', '-c', 'ssl_cert_file=/test/server.crt', '-c', 'ssl_key_file=/test/server.key'])
            port = int(dev.run(['docker', 'port', name, '5432/tcp']).strip().rsplit(':', 1)[1])
            assert port != 15432, 'Disposable test must not use the RDS tunnel port'
            report['database_port'] = port
            config = {'rds_address': 'localhost', 'local_tunnel_port': port}
            ca = str(path / 'server.crt')
            # TCP readiness excludes the image's temporary initialization server.
            for _ in range(60):
                try:
                    dev.sql(master, config, ca, 'SELECT 1;')
                    break
                except RuntimeError:
                    time.sleep(0.5)
            else:
                raise RuntimeError('Disposable server not ready')
            migrator = {'username': 'migrator_dev_01', 'password': secrets.token_urlsafe(32), 'dbname': 'istpetdev_dev_01'}
            app = {'username': 'user_dev_01', 'password': secrets.token_urlsafe(32), 'dbname': 'istpetdev_dev_01'}
            dev.sql(master, config, ca, 'CREATE DATABASE istpetdev_dev_01;')
            dev.sql(master, config, ca, f"""
CREATE ROLE migrator_dev_01 LOGIN NOSUPERUSER NOBYPASSRLS NOINHERIT PASSWORD {dev.literal(migrator['password'])};
CREATE ROLE user_dev_01 LOGIN NOSUPERUSER NOBYPASSRLS NOINHERIT PASSWORD {dev.literal(app['password'])};
REVOKE ALL ON DATABASE istpetdev_dev_01 FROM PUBLIC;
GRANT CONNECT ON DATABASE istpetdev_dev_01 TO migrator_dev_01,user_dev_01;
""")
            dev.sql(master, config, ca, """
CREATE EXTENSION IF NOT EXISTS postgis;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE,CREATE ON SCHEMA public TO migrator_dev_01;
GRANT USAGE ON SCHEMA public TO user_dev_01;
ALTER DEFAULT PRIVILEGES FOR ROLE migrator_dev_01 IN SCHEMA public GRANT SELECT,INSERT,UPDATE,DELETE ON TABLES TO user_dev_01;
ALTER DEFAULT PRIVILEGES FOR ROLE migrator_dev_01 IN SCHEMA public GRANT USAGE,SELECT ON SEQUENCES TO user_dev_01;
""", 'istpetdev_dev_01')
            report['server'] = dev.sql(master, config, ca, "SELECT current_setting('server_version'),postgis_lib_version();", 'istpetdev_dev_01')
            env = {**os.environ, 'SCHEMA95_APP_CONNECTION': dev.npgsql(app, config, ca), 'SCHEMA95_MIGRATOR_CONNECTION': dev.npgsql(migrator, config, ca)}
            result = subprocess.run(['dotnet', 'run', '--project', str(dev.ROOT / 'tools/Schema95RuntimeCheck'), '--no-launch-profile'], env=env, cwd=dev.ROOT, text=True, capture_output=True)
            log = result.stdout + result.stderr
            assert not any(value['password'] in log for value in [master, migrator, app]), 'Test output contained a credential'
            dev.write_private(dev.PRIVATE / 'schema95-runtime.log', log)
            report['exit_code'] = result.returncode
            assert result.returncode == 0, 'Runtime checks failed; inspect private schema95-runtime.log'
            runtime = next(json.loads(line) for line in result.stdout.splitlines() if line.startswith('{"result":"schema95_runtime_checks_passed"'))
            report.update(runtime)
            report['source_hashes'] = {str(file.relative_to(dev.ROOT)): hashlib.sha256(file.read_bytes()).hexdigest() for file in [Path(__file__).resolve(), dev.ROOT / 'tools/Schema95RuntimeCheck/Program.cs', dev.ROOT / 'src/Api/Authentication/OrganizationContextFilter.cs', dev.ROOT / 'src/Api/Controllers/ServicePointsController.cs']}
            print(json.dumps(runtime))
        finally:
            cleanup = subprocess.run(['docker', 'rm', '-f', '-v', name], capture_output=True)
            report['container_removed'] = cleanup.returncode == 0
            dev.write_private(dev.PRIVATE / 'schema95-runtime-verification.json', json.dumps(report, indent=2))
            print('Disposable container removed:', report['container_removed'])
            if cleanup.returncode:
                raise RuntimeError('Disposable container cleanup failed')


if __name__ == '__main__':
    main()
