#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DUMP="$ROOT/egeszsegkalauz.app/dev-data/dev.dump"
DB="egeszsegkalauz"

usage() {
  cat >&2 <<'USAGE'
Usage: ./dev-db.sh <command>
  dump    snapshot the running dev DB into egeszsegkalauz.app/dev-data/dev.dump (restored on every API start)
  reset   delete the dump; next start is migrations + DevDataSeeder only
  psql    open psql against the running dev DB
USAGE
  exit 1
}

container() {
  local id
  id="$(docker ps -q --filter label=egeszsegkalauz.dev-db=true | head -n1)"
  if [[ -z "$id" ]]; then
    echo "No running dev database. Start the API first (dotnet run in egeszsegkalauz.app/egeszsegkalauz.api)." >&2
    exit 1
  fi
  echo "$id"
}

case "${1:-}" in
  dump)
    id="$(container)"
    mkdir -p "$(dirname "$DUMP")"
    docker exec "$id" pg_dump -U postgres -d "$DB" --format=custom > "$DUMP.tmp"
    mv "$DUMP.tmp" "$DUMP"
    echo "Wrote $DUMP ($(du -h "$DUMP" | cut -f1)). It is restored on every API start."
    ;;
  reset)
    rm -f "$DUMP"
    echo "Removed $DUMP. Next start: empty DB -> migrations -> DevDataSeeder."
    ;;
  psql)
    id="$(container)"
    docker exec -it "$id" psql -U postgres -d "$DB"
    ;;
  *) usage ;;
esac
