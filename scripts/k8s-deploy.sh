#!/usr/bin/env bash
# Staged deployment. Kubernetes cannot say "run this Job before that Deployment", so every object
# carries a commerce.io/stage label; the stages are applied in order, each one finished before the next.
# Usage: bash scripts/k8s-deploy.sh <overlay> [stage...]      stages: infra dbinit migrate apps
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

overlay="${1:?usage: k8s-deploy.sh <overlay> [stage...]}"; shift || true
stages=("$@")
if [ ${#stages[@]} -eq 0 ]; then
  stages=(infra dbinit migrate apps)
  # Local development includes the dev observability backend. Other overlays bring their own.
  [ "$overlay" = local ] && stages=(infra observability dbinit migrate apps)
fi

dir="k8s/overlays/$overlay"
ns=commerce-core

apply() { kubectl kustomize "$dir" | kubectl apply -f - -l "commerce.io/stage=$1"; }

wait_for_jobs() {
  if ! kubectl -n "$ns" wait --for=condition=complete job -l "commerce.io/stage=$1" --timeout=600s; then
    echo "A $1 job failed or timed out. Logs:"
    kubectl -n "$ns" logs -l "commerce.io/stage=$1" --tail=50 --all-containers || true
    exit 1
  fi
}

for stage in "${stages[@]}"; do
  echo "==> $stage"
  case "$stage" in
    infra)
      apply infra
      bash scripts/k8s-secrets.sh
      kubectl -n "$ns" rollout status statefulset/sqlserver --timeout=600s
      kubectl -n "$ns" rollout status statefulset/rabbitmq --timeout=300s
      ;;
    dbinit | migrate)
      # Jobs are immutable, so a re-run starts from a clean slate.
      kubectl -n "$ns" delete job -l "commerce.io/stage=$stage" --ignore-not-found --wait=true
      apply "$stage"
      wait_for_jobs "$stage"
      ;;
    observability)
      apply observability
      kubectl -n "$ns" rollout status deployment/lgtm --timeout=600s
      ;;
    apps)
      apply apps
      for d in $(kubectl -n "$ns" get deploy -l commerce.io/stage=apps -o name); do
        kubectl -n "$ns" rollout status "$d" --timeout=300s
      done
      ;;
    *)
      echo "unknown stage: $stage"; exit 1 ;;
  esac
done