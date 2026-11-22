#!/usr/bin/env bash
# Regenerates client/src/app/api from Swagger. Starts (and afterwards stops) the backend if it isn't running.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SWAGGER_URL="${SWAGGER_URL:-http://localhost:5130/swagger/v1/swagger.json}"
TIMEOUT_SEC="${TIMEOUT_SEC:-180}"
LOG="$(mktemp -t egeszsegkalauz-api.XXXXXX.log)"
backend_pid=""

swagger_ready() { curl -sf -o /dev/null --max-time 5 "$SWAGGER_URL"; }

cleanup() {
  if [[ -n "$backend_pid" ]] && kill -0 "$backend_pid" 2>/dev/null; then
    echo "Stopping backend..."
    # Negative pid: the whole setsid process group, i.e. `dotnet run` and the app it spawned.
    kill -TERM -- "-$backend_pid" 2>/dev/null || true
    wait "$backend_pid" 2>/dev/null || true
  fi
}
trap cleanup EXIT

if swagger_ready; then
  echo "Backend already serving Swagger; reusing it."
else
  echo "Starting backend (log: $LOG)..."
  setsid dotnet run --project "$ROOT/egeszsegkalauz.app/egeszsegkalauz.api" --launch-profile http >"$LOG" 2>&1 &
  backend_pid=$!

  for ((i = 0; i < TIMEOUT_SEC; i++)); do
    if ! kill -0 "$backend_pid" 2>/dev/null; then
      echo "Backend exited unexpectedly:" >&2; tail -n 30 "$LOG" >&2; exit 1
    fi
    swagger_ready && break
    sleep 1
  done
  swagger_ready || { echo "Swagger not reachable within ${TIMEOUT_SEC}s:" >&2; tail -n 30 "$LOG" >&2; exit 1; }
fi

echo "Regenerating Angular API client..."
(cd "$ROOT/client" && npx ng-openapi-gen --config open-api-gen.json --input "$SWAGGER_URL")
echo "API client regenerated."
