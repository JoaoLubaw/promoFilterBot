#!/usr/bin/env bash
# Restores a dump made by backup.sh: ./scripts/restore.sh backups/promofilterbot-20261020-040000.sql.gz
# The app is stopped meanwhile, so nothing writes while the tables are replaced.
set -euo pipefail

cd "$(dirname "$0")/.."

if [ $# -ne 1 ] || [ ! -f "$1" ]; then
    echo "Uso: $0 <arquivo .sql.gz do backup>" >&2
    exit 1
fi

docker compose stop app
gunzip -c "$1" | docker compose exec -T db sh -c 'psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
docker compose start app

echo "Backup $1 restored."
