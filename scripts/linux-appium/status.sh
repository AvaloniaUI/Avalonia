#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/common.sh"

ensure_dirs

if [[ ! -f "$STATE_FILE" ]]; then
    echo "Linux Appium environment is not running (no state file)."
    echo "Expected state file: $STATE_FILE"
    exit 1
fi

source_state_if_exists

status_line() {
    local label="$1"
    local pid="${2:-}"
    if is_pid_alive "$pid"; then
        echo "$label: running (pid $pid)"
    else
        echo "$label: stopped"
    fi
}

echo "State file: $STATE_FILE"
echo "Driver URL: ${SELENIUM_REMOTE_URL:-$DRIVER_URL}"
echo "DISPLAY: ${DISPLAY:-$DISPLAY_VALUE}"
echo "DBUS_SESSION_BUS_ADDRESS: ${DBUS_SESSION_BUS_ADDRESS:-<unset>}"
echo "Logs: ${LOG_DIR:-$STATE_DIR/logs}"
status_line "dbus-daemon" "${DBUS_DAEMON_PID:-}"
status_line "at-spi-bus-launcher" "${ATSPI_BUS_PID:-}"
status_line "at-spi2-registryd" "${ATSPI_REGISTRY_PID:-}"
status_line "AT-SPI WebDriver" "${DRIVER_PID:-}"

if [[ -n "${XVFB_PID:-}" ]]; then
    status_line "Xvfb (managed)" "${XVFB_PID:-}"
else
    if pgrep -x Xvfb >/dev/null 2>&1; then
        echo "Xvfb: running (external)"
    else
        echo "Xvfb: stopped"
    fi
fi

