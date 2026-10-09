#!/usr/bin/env python3
"""Terraform external data source: read enrollment, never credentials."""
import json
import os
import re
import subprocess
import sys

username = json.load(sys.stdin)["username"]
if not re.fullmatch(r"istpetdev-[A-Za-z0-9+=,.@_-]+", username):
    sys.exit("Invalid project username")
result = subprocess.run(
    ["aws", "iam", "list-mfa-devices", "--user-name", username, "--output", "json", "--no-cli-pager"],
    env={**os.environ, "AWS_REGION": "us-east-1", "AWS_DEFAULT_REGION": "us-east-1"},
    capture_output=True, text=True,
)
if result.returncode:
    sys.exit("Cannot verify IAM MFA enrollment; refusing to grant access")
print(json.dumps({"enabled": str(bool(json.loads(result.stdout)["MFADevices"])).lower()}))
