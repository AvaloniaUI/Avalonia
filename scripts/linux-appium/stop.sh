#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/common.sh"

kill_pid_if_alive() {
    local pid="$1"
    local name="$2"
    if ! is_pid_alive "$pid"; then
        return
    fi

    kill "$pid" >/dev/null 2>&1 || true
    sleep 1
    if is_pid_alive "$pid"; then
        kill -9 "$pid" >/dev/null 2>&1 || true
    fi
    echo "Stopped $name ($pid)"
}

ensure_dirs
source_state_if_exists

kill_pid_if_alive "${DRIVER_PID:-}" "AT-SPI WebDriver"
kill_pid_if_alive "${ATSPI_REGISTRY_PID:-}" "at-spi2-registryd"
kill_pid_if_alive "${ATSPI_BUS_PID:-}" "at-spi-bus-launcher"
kill_pid_if_alive "${DBUS_DAEMON_PID:-}" "dbus-daemon"

if [[ -n "${XVFB_PID:-}" ]]; then
    kill_pid_if_alive "${XVFB_PID:-}" "Xvfb"
fi

if [[ -n "${DRIVER_PORT:-}" ]]; then
    if command -v pgrep >/dev/null 2>&1; then
        leftover_driver="$(pgrep -f "python3 -m flask run --host ${DRIVER_HOST:-127.0.0.1} --port ${DRIVER_PORT}" || true)"
        if [[ -n "$leftover_driver" ]]; then
            for pid in $leftover_driver; do
                kill_pid_if_alive "$pid" "AT-SPI WebDriver (leftover)"
            done
        fi
    fi
fi

rm -f "$STATE_FILE"
echo "Linux Appium environment stopped."
echo "State file removed: $STATE_FILE"

