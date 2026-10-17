#!/usr/bin/env bash
# Creates a local K3s-in-Docker cluster and loads this repo's images into it.
# Usage: bash scripts/k3d-up.sh [all|migrators]
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

CLUSTER=commerce

if ! k3d cluster list "$CLUSTER" >/dev/null 2>&1; then
  k3d cluster create "$CLUSTER" --agents 0 -p "8088:80@loadbalancer" --wait
fi
kubectl config use-context "k3d-$CLUSTER" >/dev/null

mapfile -t images < <(bash scripts/build-images.sh "${1:-all}")
k3d image import "${images[@]}" -c "$CLUSTER"
echo "Cluster '$CLUSTER' is ready; ${#images[@]} images imported."