#!/usr/bin/env bash
# End-to-end smoke test against a running platform (local cluster or CI cluster).
# Usage: bash scripts/smoke-test.sh [base-url] [host-header]
set -euo pipefail

BASE="${1:-http://127.0.0.1:8088}"
HOST="${2:-commerce.localhost}"
KEYBOARD=11111111-1111-1111-1111-111111111111
MONITOR=33333333-3333-3333-3333-333333333333
CUSTOMER=3fa85f64-5717-4562-b3fc-2c963f66afa6

req() { curl -sS -H "Host: $HOST" "$@"; }
fail() { echo "FAIL: $*" >&2; exit 1; }

curl -sS -o /dev/null --max-time 5 -H "Host: $HOST" "$BASE/health/live" \
  || fail "cannot reach $BASE. Is the platform running? (cluster: kubectl -n commerce-core get pods | compose: docker compose ps)"

# Polls a URL until the body matches a regular expression.
wait_for() {   # description, url, pattern, [attempts]
  local what="$1" url="$2" pattern="$3" attempts="${4:-60}"
  for _ in $(seq 1 "$attempts"); do
    # No grep -q: closing the pipe early makes curl fail, and pipefail would turn a match into a failure.
    if req "$url" 2>/dev/null | grep -E "$pattern" >/dev/null; then
      echo "ok: $what"
      return 0
    fi
    sleep 2
  done
  fail "$what (timed out after $((attempts * 2))s)"
}

place_order() {   # product, quantity, payment token -> prints the order id
  local response
  response=$(req -i -X POST -H "Content-Type: application/json" "$BASE/api/v1/orders" \
    -d "{\"customerId\":\"$CUSTOMER\",\"paymentToken\":\"$3\",\"items\":[{\"productId\":\"$1\",\"quantity\":$2}]}")

  [[ "$(printf '%s' "$response" | head -n1)" == *" 201"* ]] || fail "order was not accepted: $response"
  printf '%s' "$response" | grep -i '^location:' | tr -d '\r' | sed 's#.*/##'
}

wait_for "gateway is ready" "$BASE/health/ready" "Healthy" 60
wait_for "catalog is seeded" "$BASE/api/v1/products" "KB-001" 30

# 1. Happy path: reserve stock, charge, confirm, notify
order=$(place_order "$KEYBOARD" 1 tok_ok)
wait_for "order $order is paid" "$BASE/api/v1/orders/$order" '"status":"Paid"'
wait_for "payment was recorded" "$BASE/api/v1/payments?orderId=$order" '"status":"Succeeded"'
wait_for "confirmation was sent" "$BASE/api/v1/notifications?orderId=$order" 'OrderConfirmed'

# 2. Declined payment: the stock reservation is released and the order is cancelled
declined=$(place_order "$KEYBOARD" 1 tok_decline)
wait_for "declined order $declined is cancelled" "$BASE/api/v1/orders/$declined" '"status":"Cancelled"'
wait_for "failed payment was recorded" "$BASE/api/v1/payments?orderId=$declined" '"status":"Failed"'
wait_for "rejection was sent" "$BASE/api/v1/notifications?orderId=$declined" 'OrderRejected'

# 3. Out of stock (only 5 monitors exist): cancelled without ever reaching payment
short=$(place_order "$MONITOR" 6 tok_ok)
wait_for "out-of-stock order $short is cancelled" "$BASE/api/v1/orders/$short" '"status":"Cancelled"'

# 4. Internal endpoints must not be reachable from outside
code=$(req -o /dev/null -w '%{http_code}' "$BASE/internal/v1/prices?ids=$KEYBOARD")
[ "$code" = "404" ] || fail "/internal must not be exposed, got HTTP $code"
echo "ok: /internal is not exposed"

echo "All smoke checks passed."