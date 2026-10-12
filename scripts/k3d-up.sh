#!/usr/bin/env bash
# Creates a local K3s-in-Docker cluster and loads this repo's images into it.
# Usage: bash scripts/k3d-up.sh [all|migrators]
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

CLUSTER=commerce
WHAT="${1:-all}"

if ! k3d cluster list "$CLUSTER" >/dev/null 2>&1; then
  k3d cluster create "$CLUSTER" --agents 0 -p "8088:80@loadbalancer" --wait
fi
kubectl config use-context "k3d-$CLUSTER" >/dev/null

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

images=()
for name in "${!MIGRATORS[@]}"; do
  docker build -f "${MIGRATORS[$name]}" --target migrator -t "commerce/$name:dev" .
  images+=("commerce/$name:dev")
done

if [ "$WHAT" = "all" ]; then
  for name in "${!APPS[@]}"; do
    docker build -f "${APPS[$name]}" -t "commerce/$name:dev" .
    images+=("commerce/$name:dev")
  done
fi

k3d image import "${images[@]}" -c "$CLUSTER"
echo "Cluster '$CLUSTER' is ready; ${#images[@]} images imported."