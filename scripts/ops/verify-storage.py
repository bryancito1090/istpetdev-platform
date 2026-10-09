#!/usr/bin/env python3
"""Verify AWS bucket controls and recover an earlier version of a synthetic object."""
import argparse
import hashlib
import json
from pathlib import Path
import secrets
import tempfile
import dev

p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--profile', required=True)
p.add_argument('--outputs', required=True)
a = p.parse_args()
aws = dev.Aws(a.profile)
o = {k: v['value'] for k, v in json.loads(Path(a.outputs).read_text()).items()}
account = aws.call('sts', 'get-caller-identity')['Account']
state = f'istpetdev-tfstate-{account}-us-east-1'
for bucket in [state, o['evidence_bucket_name'], o['backup_bucket_name']]:
    assert all(aws.call('s3api', 'get-public-access-block', bucket=bucket)['PublicAccessBlockConfiguration'].values())
    assert aws.call('s3api', 'get-bucket-versioning', bucket=bucket)['Status'] == 'Enabled'
    assert aws.call('s3api', 'get-bucket-ownership-controls', bucket=bucket)['OwnershipControls']['Rules'][0]['ObjectOwnership'] == 'BucketOwnerEnforced'
    rules = aws.call('s3api', 'get-bucket-encryption', bucket=bucket)['ServerSideEncryptionConfiguration']['Rules']
    assert rules[0]['ApplyServerSideEncryptionByDefault']['SSEAlgorithm'] == ('aws:kms' if bucket == state else 'AES256')
    policy = json.loads(aws.call('s3api', 'get-bucket-policy', bucket=bucket)['Policy'])
    assert any(s['Effect'] == 'Deny' and s.get('Condition', {}).get('Bool', {}).get('aws:SecureTransport') == 'false' for s in policy['Statement'])
    print('PASS: bucket purpose ' + ('state' if bucket == state else 'backups' if bucket == o['backup_bucket_name'] else 'evidence') + ' private/versioned/encrypted/TLS-only')
key = 'dev_01/_validation/' + secrets.token_hex(12)
versions = []
bucket = o['evidence_bucket_name']
with tempfile.TemporaryDirectory(prefix='istpetdev-storage-test-') as directory:
    path = Path(directory)/'synthetic.txt'
    try:
        for body in ['synthetic evidence version 1\n', 'synthetic evidence version 2\n']:
            path.write_text(body)
            versions.append(aws.call('s3api', 'put-object', bucket=bucket, key=key, body=str(path), checksum_algorithm='SHA256')['VersionId'])
        versions.append(aws.call('s3api', 'delete-object', bucket=bucket, key=key)['VersionId'])
        # CLI get-object has a positional output path; write it privately, never log object content.
        dev.run(['aws', 's3api', 'get-object', '--bucket', bucket, '--key', key, '--version-id', versions[0], str(path), '--no-cli-pager'], env=aws.env)
        assert path.read_text() == 'synthetic evidence version 1\n'
        print('PASS: previous evidence version recovered after overwrite and delete marker')
    finally:
        for version in versions:
            aws.call('s3api', 'delete-object', bucket=bucket, key=key, version_id=version)
    versions_state = aws.call('s3api', 'list-object-versions', bucket=state, prefix='bootstrap/terraform.tfstate').get('Versions', [])
    versions_state = [v for v in versions_state if v['Key'] == 'bootstrap/terraform.tfstate']
    if not versions_state:
        raise RuntimeError('No bootstrap state version found')
    selected = versions_state[0]
    dev.run(['aws', 's3api', 'get-object', '--bucket', state, '--key', selected['Key'], '--version-id', selected['VersionId'], str(path), '--no-cli-pager'], env=aws.env)
    restored = json.loads(path.read_text())
    assert len([r for r in restored['resources'] if r['mode'] == 'managed']) == 6
    print('PASS: bootstrap state recovered privately from a specific S3 version (live state unchanged)')
