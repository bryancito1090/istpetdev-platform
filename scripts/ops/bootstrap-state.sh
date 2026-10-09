#!/usr/bin/env bash
# Run from the repository root. Explicit phases keep reviewed plans separate from apply.
set -euo pipefail
umask 077
phase=${1:---help}
private_dir=${ISTPETDEV_PRIVATE_DIR:-"$HOME/.config/istpetdev/private"}
root=$(cd "$(dirname "$0")/../.." && pwd)
stack="$root/infra/terraform/bootstrap"
export AWS_PROFILE=${AWS_PROFILE:-istpetdev-admin}
export AWS_REGION=us-east-1 AWS_DEFAULT_REGION=us-east-1 AWS_PAGER=''
export TF_DATA_DIR="$private_dir/terraform-bootstrap"
mkdir -p "$private_dir" "$TF_DATA_DIR"
chmod 700 "$private_dir" "$TF_DATA_DIR"
case "$phase" in
  --help)
    echo 'Usage: bootstrap-state.sh prepare|apply|migrate'
    echo 'Set EXPIRES_AT=YYYY-MM-DD. Review private/bootstrap.tfplan before apply.'
    echo 'prepare checks AWS, reuses alias/istpetdev-tfstate, and creates only a plan.'
    echo 'Existing buckets without this state require explicit import/review; never overwritten.'
    exit 0 ;;
  prepare)
    : "${EXPIRES_AT:?Set the agreed expiry date}"
    account=$(aws sts get-caller-identity --query Account --output text --no-cli-pager)
    key=$(aws kms describe-key --key-id alias/istpetdev-tfstate --query KeyMetadata.Arn --output text --no-cli-pager)
    bucket="istpetdev-tfstate-$account-us-east-1"
    if [[ ! -e "$private_dir/bootstrap.tfstate" ]]; then
      owned=$(aws s3api list-buckets --query "Buckets[?Name=='$bucket'].Name" --output text --no-cli-pager)
      if [[ -n "$owned" ]]; then
        echo 'Bucket already exists. Inspect its policy/state and import before preparing changes.' >&2
        exit 1
      fi
    fi
    if [[ -f "$TF_DATA_DIR/terraform.tfstate" ]] && jq -e '.backend.type == "s3"' "$TF_DATA_DIR/terraform.tfstate" >/dev/null; then
      echo 'Bootstrap already uses S3. Use normal Terraform plan with its private backend configuration.' >&2
      exit 1
    fi
    jq -n --arg expiry "$EXPIRES_AT" '{expires_at:$expiry}' > "$private_dir/bootstrap.tfvars.json"
    # Terraform explicitly supports replacing the backend through an override file.
    python3 - "$stack/backend_override.tf" "$private_dir/bootstrap.tfstate" <<'PY'
import json,sys
from pathlib import Path
Path(sys.argv[1]).write_text('terraform {\n  backend "local" {\n    path = '+json.dumps(sys.argv[2])+'\n  }\n}\n')
PY
    for env in bootstrap dev; do
      cat > "$private_dir/backend-$env.hcl" <<EOF
bucket       = "$bucket"
key          = "$env/terraform.tfstate"
region       = "us-east-1"
encrypt      = true
kms_key_id   = "$key"
use_lockfile = true
EOF
    done
    terraform -chdir="$stack" init -input=false -no-color
    terraform -chdir="$stack" plan -input=false -no-color -var-file="$private_dir/bootstrap.tfvars.json" -out="$private_dir/bootstrap.tfplan"
    ;;
  apply)
    terraform -chdir="$stack" apply -input=false -no-color "$private_dir/bootstrap.tfplan"
    ;;
  migrate)
    test -s "$private_dir/bootstrap.tfstate"
    rm -f "$stack/backend_override.tf"
    terraform -chdir="$stack" init -migrate-state -force-copy -input=false -no-color -backend-config="$private_dir/backend-bootstrap.hcl"
    terraform -chdir="$stack" state pull > "$private_dir/bootstrap-migrated.tfstate"
    python3 - "$private_dir/bootstrap.tfstate" "$private_dir/bootstrap-migrated.tfstate" <<'PY'
import json,sys
old,new=(json.load(open(p)) for p in sys.argv[1:])
assert old['resources']==new['resources'] and old['outputs']==new['outputs'], 'State migration mismatch'
print('State migration verified; original private copy retained.')
PY
    ;;
  *) echo 'Unknown phase. Use --help.' >&2; exit 2 ;;
esac
