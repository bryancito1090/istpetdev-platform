#!/usr/bin/env python3
"""Run on each member's machine with their own login and their active SSM tunnel."""
import argparse
import json
from pathlib import Path
import dev

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--profile', required=True)
p.add_argument('--member', choices=dev.MEMBERS, required=True)
p.add_argument('--config', required=True)
a = p.parse_args()
c = json.loads(Path(a.config).read_text())
aws = dev.Aws(a.profile)
ca = dev.ca_file()
other = next(m for m in dev.MEMBERS if m != a.member)
for key in [a.member, a.member + '/migrator', 'integration/' + a.member]:
    response = aws.call('secretsmanager', 'get-secret-value', secret_id='/istpetdev/dev/db/' + key)
    secret = json.loads(response['SecretString'])
    dev.sql(secret, c, ca, 'SELECT 1;')
    assert dev.sql(secret, c, ca, 'SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid();') == 't'
    try:
        dev.sql(secret, c, ca, 'SELECT 1;', 'istpetdev_' + other)
    except RuntimeError:
        pass
    else:
        raise AssertionError('Cross-database access succeeded')
for key in [other, other + '/migrator', 'integration/migrator', 'integration/backup']:
    try:
        aws.call('secretsmanager', 'get-secret-value', secret_id='/istpetdev/dev/db/' + key)
    except dev.AwsError as e:
        if e.code not in ['AccessDeniedException', 'AccessDenied']:
            raise
    else:
        raise AssertionError('An unassigned secret was readable')
print('PASS: own app/migration/integration credentials and TLS; foreign secrets/databases denied.')
print('Run this from a second member identity too. IAM policy simulation alone is not this test.')
