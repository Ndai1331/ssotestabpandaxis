#!/bin/bash
# Daily clone qcadmin DB from prod (206.189.147.187) to local MySQL container
# Creates: qcadmin_stag + qcadmin_test
# Usage: bash /home/tobi/task9-workspace/scripts/daily-clone-qcadmin.sh
# Add to crontab: 0 3 * * * /home/tobi/task9-workspace/scripts/daily-clone-qcadmin.sh >> /home/tobi/task9-workspace/scripts/clone.log 2>&1

set -euo pipefail

# ─── Config ───────────────────────────────────────────────────
REMOTE_HOST="206.189.147.187"
REMOTE_PORT="3306"
REMOTE_DB="qcadmin_test"
REMOTE_USER="qcadmin_test"
REMOTE_PASS="${REMOTE_PASS:?set in env}"

LOCAL_CONTAINER="task9-mysql"
LOCAL_ROOT_PASS="${LOCAL_ROOT_PASS:-root123}"
LOCAL_DBS=("qcadmin_stag" "qcadmin_test")

WORKSPACE="/home/tobi/task9-workspace"
DUMP_DIR="${WORKSPACE}/backups"
LOG_FILE="${WORKSPACE}/scripts/clone.log"
KEEP_DAYS=7

TIMESTAMP=$(date '+%Y%m%d_%H%M%S')
DUMP_FILE="${DUMP_DIR}/qcadmin_${TIMESTAMP}.sql"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" | tee -a "${LOG_FILE}"
}

# ─── Ensure dump dir ────────────────────────────────────────────
mkdir -p "${DUMP_DIR}"

log "===== Daily clone started ====="

# ─── Step 1: Verify source DB reachable ──────────────────────────
if ! docker exec -i "${LOCAL_CONTAINER}" mysql -h"${REMOTE_HOST}" -P"${REMOTE_PORT}" -u"${REMOTE_USER}" -p"${REMOTE_PASS}" -e "SELECT 1;" >/dev/null 2>&1; then
    log "ERROR: Cannot connect to source DB at ${REMOTE_HOST}:${REMOTE_PORT}"
    exit 1
fi
log "Source DB reachable: ${REMOTE_HOST}:${REMOTE_PORT}/${REMOTE_DB}"

# ─── Step 2: Verify local MySQL container running ──────────────
if ! docker ps --format "{{.Names}}" | grep -q "^${LOCAL_CONTAINER}$"; then
    log "ERROR: Local MySQL container '${LOCAL_CONTAINER}' is not running"
    exit 1
fi
log "Local MySQL container running: ${LOCAL_CONTAINER}"

# ─── Step 3: Dump source DB (strip DEFINER to avoid local user missing) ─
log "Starting mysqldump from ${REMOTE_DB}..."
TMP_DUMP="${DUMP_DIR}/qcadmin_${TIMESTAMP}_raw.sql"

docker exec -i "${LOCAL_CONTAINER}" mysqldump \
    -h"${REMOTE_HOST}" -P"${REMOTE_PORT}" \
    -u"${REMOTE_USER}" -p"${REMOTE_PASS}" \
    --single-transaction \
    --routines \
    --triggers \
    --events \
    --no-tablespaces \
    --set-gtid-purged=OFF \
    --max-allowed-packet=512M \
    --net-buffer-length=1048576 \
    "${REMOTE_DB}" > "${TMP_DUMP}"

if [ $? -ne 0 ]; then
    log "ERROR: mysqldump failed"
    rm -f "${TMP_DUMP}"
    exit 1
fi

sed 's/DEFINER=[^*]*\*/\*/g; s/SQL SECURITY DEFINER/SQL SECURITY INVOKER/g' "${TMP_DUMP}" > "${DUMP_FILE}"
rm -f "${TMP_DUMP}"

DUMP_SIZE=$(du -h "${DUMP_FILE}" | cut -f1)
log "Dump complete: ${DUMP_FILE} (${DUMP_SIZE})"

# ─── Step 4: Optimize MySQL for bulk import ────────────────────
docker exec -i "${LOCAL_CONTAINER}" mysql -uroot -p"${LOCAL_ROOT_PASS}" -e \
    "SET GLOBAL innodb_flush_log_at_trx_commit=2; SET GLOBAL innodb_buffer_pool_size=268435456;" \
    >/dev/null 2>&1
log "MySQL optimized for bulk import"

# ─── Step 5: Import into each local DB ─────────────────────────
for DB_NAME in "${LOCAL_DBS[@]}"; do
    log "--- Processing ${DB_NAME} ---"

    # Drop/create
    docker exec -i "${LOCAL_CONTAINER}" mysql -uroot -p"${LOCAL_ROOT_PASS}" -e \
        "DROP DATABASE IF EXISTS ${DB_NAME}; CREATE DATABASE ${DB_NAME} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;" \
        >/dev/null 2>&1
    log "Recreated: ${DB_NAME}"

    # Import
    log "Importing into ${DB_NAME}..."
    if ! docker exec -i "${LOCAL_CONTAINER}" mysql -uroot -p"${LOCAL_ROOT_PASS}" \
        --init-command="SET SESSION FOREIGN_KEY_CHECKS=0; SET SESSION UNIQUE_CHECKS=0; SET SESSION AUTOCOMMIT=0;" \
        "${DB_NAME}" < "${DUMP_FILE}"; then
        log "ERROR: Import failed for ${DB_NAME}"
        exit 1
    fi

    # Verify
    TABLE_COUNT=$(docker exec -i "${LOCAL_CONTAINER}" mysql -uroot -p"${LOCAL_ROOT_PASS}" -s -N \
        -e "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='${DB_NAME}';" 2>/dev/null)
    log "Import complete. Tables in ${DB_NAME}: ${TABLE_COUNT}"

    # Re-grant write perms to root@% (DROP DATABASE wiped schema-level grants).
    # API container connects as root@<container-ip> and needs DML for login (UPDATE users).
    docker exec -i "${LOCAL_CONTAINER}" mysql -uroot -p"${LOCAL_ROOT_PASS}" -e \\
        "GRANT SELECT, INSERT, UPDATE, DELETE ON ${DB_NAME}.* TO 'root'@'%'; FLUSH PRIVILEGES;" >/dev/null 2>&1
    log "Granted write perms on ${DB_NAME} to root@%"
done

# ─── Step 6: Cleanup old dumps ──────────────────────────────────
DELETED=$(find "${DUMP_DIR}" -name "qcadmin_*.sql" -mtime +${KEEP_DAYS} -delete -print | wc -l)
log "Cleaned up ${DELETED} old dump files (> ${KEEP_DAYS} days)"

log "===== Daily clone finished successfully ====="
