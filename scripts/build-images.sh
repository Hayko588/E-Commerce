#!/usr/bin/env bash
# Builds the platform images as commerce/<name>:dev and prints their names, one per line.
# Build output goes to stderr so stdout stays machine-readable.
# Usage: bash scripts/build-images.sh [all|migrators]
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

WHAT="${1:-all}"

declare -A MIGRATORS=(
  [ordering-migrator]=src/Services/CommerceCore.Ordering.Api/Dockerfile
  [catalog-migrator]=src/Services/CommerceCore.Catalog.Api/Dockerfile
  [payments-migrator]=src/Services/CommerceCore.Payments.Api/Dockerfile
  [notifications-migrator]=src/Services/CommerceCore.Notifications.Api/Dockerfile
)
declare -A APPS=(
  [ordering-api]=src/Services/CommerceCore.Ordering.Api/Dockerfile
  [catalog-api]=src/Services/CommerceCore.Catalog.Api/Dockerfile
  [payments-api]=src/Services/CommerceCore.Payments.Api/Dockerfile
  [notifications-api]=src/Services/CommerceCore.Notifications.Api/Dockerfile
  [gateway]=src/Gateway/CommerceCore.Gateway/Dockerfile
)

for name in "${!MIGRATORS[@]}"; do
  docker build -f "${MIGRATORS[$name]}" --target migrator -t "commerce/$name:dev" . >&2
  echo "commerce/$name:dev"
done

if [ "$WHAT" = "all" ]; then
  for name in "${!APPS[@]}"; do
    docker build -f "${APPS[$name]}" -t "commerce/$name:dev" . >&2
    echo "commerce/$name:dev"
  done
fi