#!/usr/bin/env bash
# Creates the commerce-secrets Secret with random passwords. Nothing here is ever committed.
# Usage: bash scripts/k8s-secrets.sh [--force]
set -euo pipefail
NS="${NAMESPACE:-commerce-core}"

if kubectl -n "$NS" get secret commerce-secrets >/dev/null 2>&1 && [ "${1:-}" != "--force" ]; then
  echo "commerce-secrets already exists (use --force to replace it)."
  exit 0
fi

# Meets SQL Server's complexity rules: upper, lower, digit and a symbol.
pw() { echo "Aa1-$(openssl rand -hex 16)"; }

args=(
  --from-literal="MSSQL_SA_PASSWORD=$(pw)"
  --from-literal="RABBITMQ_PASSWORD=$(pw)"
)

declare -A DB=( [orders]=OrdersDb [catalog]=CatalogDb [payments]=PaymentsDb [notifications]=NotificationsDb )
for svc in orders catalog payments notifications; do
  app="$(pw)"; mig="$(pw)"; db="${DB[$svc]}"; upper="${svc^^}"
  args+=(
    --from-literal="${upper}_APP_PASSWORD=$app"
    --from-literal="${upper}_MIGRATOR_PASSWORD=$mig"
    --from-literal="${upper}_DB_CONN=Server=sqlserver;Database=$db;User Id=${svc}_app;Password=$app;TrustServerCertificate=True"
    --from-literal="${upper}_MIGRATOR_CONN=Server=sqlserver;Database=$db;User Id=${svc}_migrator;Password=$mig;TrustServerCertificate=True"
  )
done

kubectl -n "$NS" create secret generic commerce-secrets "${args[@]}" --dry-run=client -o yaml | kubectl apply -f -
echo "commerce-secrets created in namespace $NS."