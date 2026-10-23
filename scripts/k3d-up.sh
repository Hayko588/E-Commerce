#!/usr/bin/env bash
# Creates (or starts) the local K3s-in-Docker cluster and loads this repo's images into it.
# Usage: bash scripts/k3d-up.sh [all|migrators|start]
#   all        build every image and import them (default)
#   migrators  build and import only the four migration images
#   start      only make sure the cluster exists and is running (no builds)
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

CLUSTER=commerce
WHAT="${1:-all}"
SERVER="k3d-$CLUSTER-server-0"

if ! k3d cluster list "$CLUSTER" >/dev/null 2>&1; then
  # A fixed API port keeps the kubeconfig valid across stop/start.
  k3d cluster create "$CLUSTER" --agents 0 --api-port 127.0.0.1:6550 -p "8088:80@loadbalancer" --wait
elif [ "$(docker inspect -f '{{.State.Running}}' "$SERVER" 2>/dev/null)" != "true" ]; then
  # A stopped cluster still "exists", but nothing can connect to it until it is started.
  k3d cluster start "$CLUSTER" --wait
fi

# The API port can change when a cluster restarts, so refresh the kubeconfig entry, then wait for the node.
k3d kubeconfig merge "$CLUSTER" --kubeconfig-merge-default --kubeconfig-switch-context >/dev/null
kubectl wait --for=condition=Ready node --all --timeout=180s

if [ "$WHAT" = start ]; then
  echo "Cluster '$CLUSTER' is running."
  exit 0
fi

mapfile -t images < <(bash scripts/build-images.sh "$WHAT")
k3d image import "${images[@]}" -c "$CLUSTER"
echo "Cluster '$CLUSTER' is ready; ${#images[@]} images imported."