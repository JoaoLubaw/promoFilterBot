#!/usr/bin/env bash
# Daily dump of the database, keeping the last BACKUP_KEEP_DAYS days, with an optional copy out of the VM (rclone).
# Cron on the host, at 4 a.m.:
#   0 4 * * * cd /opt/promofilterbot && ./scripts/backup.sh >> backups/backup.log 2>&1
set -euo pipefail

cd "$(dirname "$0")/.."

read_env() {
    grep -E "^$1=" .env 2>/dev/null | tail -n 1 | cut -d= -f2- || true
}

BACKUP_DIR="${BACKUP_DIR:-./backups}"
KEEP_DAYS="${BACKUP_KEEP_DAYS:-$(read_env BACKUP_KEEP_DAYS)}"
KEEP_DAYS="${KEEP_DAYS:-7}"
RCLONE_REMOTE="${RCLONE_REMOTE:-$(read_env RCLONE_REMOTE)}"

mkdir -p "$BACKUP_DIR"
FILE="$BACKUP_DIR/promofilterbot-$(date +%Y%m%d-%H%M%S).sql.gz"

# The credentials are the ones of the database container itself
docker compose exec -T db sh -c 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --clean --if-exists' | gzip > "$FILE"
echo "$(date -Is) backup saved in $FILE ($(du -h "$FILE" | cut -f1))"

find "$BACKUP_DIR" -name 'promofilterbot-*.sql.gz' -mtime +"$KEEP_DAYS" -delete

if [ -n "$RCLONE_REMOTE" ]; then
    rclone copy "$FILE" "$RCLONE_REMOTE"
    rclone delete --min-age "${KEEP_DAYS}d" --include 'promofilterbot-*.sql.gz' "$RCLONE_REMOTE"
    echo "$(date -Is) backup copied to $RCLONE_REMOTE"
fi
