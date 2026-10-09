#!/usr/bin/env python3
"""Update private tfvars with MFA-enrolled developers; does not apply IAM changes."""
import argparse
import json
from pathlib import Path
import dev

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--profile', required=True)
p.add_argument('--roster', required=True, help='Private JSON: dev_01..05 to existing IAM usernames')
p.add_argument('--vars', required=True, help='Private dev.tfvars.json to update')
a = p.parse_args()
aws = dev.Aws(a.profile)
roster = json.loads(Path(a.roster).read_text())
variables = json.loads(Path(a.vars).read_text())
verified = {}
for member, username in roster.items():
    if member not in dev.MEMBERS or not username.startswith('istpetdev-'):
        raise ValueError('Invalid project roster')
    if username == 'istpetdev-bryan':
        print(member + ': infrastructure administrator; not represented as an isolated IAM developer')
        continue
    enrolled = bool(aws.call('iam', 'list-mfa-devices', user_name=username)['MFADevices'])
    if enrolled:
        verified[member] = username
    print(member + ': ' + ('MFA verified, proposed for limited grants' if enrolled else 'MFA absent, no data grants'))
variables['team_users'] = verified
dev.write_private(a.vars, json.dumps(variables, indent=2))
print('Private variables updated. Review Terraform plan before apply; MFA is checked again during planning.')
