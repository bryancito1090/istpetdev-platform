#!/usr/bin/env bash
set -euo pipefail

# Explicit personal operation; integration is promoted separately after merging develop.
repo_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_dir"
profile="${1:-istpetdev-admin}"
member="${2:-dev_01}"
case "$member" in dev_01|dev_02|dev_03|dev_04|dev_05) ;; *) echo 'Invalid member' >&2; exit 2 ;; esac
export PATH="$HOME/.dotnet:$HOME/.local/bin:$PATH"
export ASPNETCORE_ENVIRONMENT=Development
export DOTNET_ENVIRONMENT=Development
config="$HOME/.config/istpetdev/private/dev-access.json"
dotnet tool restore
dotnet restore
dotnet build --no-restore
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1 dotnet test tests/IstpetDev.Tests --no-build --filter "FullyQualifiedName~LeanSchemaTests|FullyQualifiedName~MigrationSafetyTests"

restore_application() {
    result=$?
    trap - EXIT
    if ! bash scripts/ops/load-dev-secrets.sh --profile "$profile" --member "$member" --database personal --config "$config"; then
        echo 'Could not restore application credentials. Do not start the API; load personal credentials without --migrator.' >&2
        if [ "$result" -eq 0 ]; then result=1; fi
    fi
    exit "$result"
}
trap restore_application EXIT
bash scripts/ops/load-dev-secrets.sh --profile "$profile" --member "$member" --database personal --migrator --config "$config"
dotnet ef migrations list --project src/Infrastructure --startup-project src/Api --no-build
dotnet ef database update --project src/Infrastructure --startup-project src/Api --no-build
echo "Personal migration completed for istpetdev_$member. Refresh public > Tables in DBeaver."
