#!/usr/bin/env bash

set -euo pipefail

FILE_PATH="/home/user/project/links.txt"

CRON_JOB="0 * * * * curl -f -s -X POST -H 'Content-Type: text/plain' --data-binary \"@$FILE_PATH\" http://localhost:8080/chunks/IndexAll && rm \"$FILE_PATH\""

echo "Adding cron job..."

# Проверяем существует ли уже такая задача
if crontab -l 2>/dev/null | grep -F "$CRON_JOB" >/dev/null; then
    echo "Cron job already exists. Skipping."
    exit 0
fi

# Добавляем задачу
(
    crontab -l 2>/dev/null
    echo "$CRON_JOB"
) | crontab -

echo "Cron job successfully added."