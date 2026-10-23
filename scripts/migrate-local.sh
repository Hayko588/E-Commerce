#!/usr/bin/env bash
# Applies every service's EF migrations to the local SQL Server (the one from docker compose).
# Use it when you run a service from the IDE. Connection strings come from each project's user-secrets.
# Usage: docker compose up -d sqlserver && bash scripts/migrate-local.sh
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

dotnet tool restore >/dev/null
for svc in Ordering Catalog Payments Notifications; do
  echo "==> $svc"
  dotnet ef database update \
    --project "src/Services/CommerceCore.$svc.Infrastructure" \
    --startup-project "src/Services/CommerceCore.$svc.Api" \
    -- --environment Development
done