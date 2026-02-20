#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/common.sh"

require_command python3
require_command dbus-daemon
require_command dotnet
require_command Xvfb
ensure_dirs
ensure_driver_repo

DRIVER_PATCH_PATH="$REPO_ROOT/scripts/linux-appium/patches/selenium-webdriver-at-spi.patch"
if [[ -f "$DRIVER_PATCH_PATH" ]]; then
    require_command git
    if git -C "$DRIVER_DIR" apply --reverse --check "$DRIVER_PATCH_PATH" >/dev/null 2>&1; then
        echo "AT-SPI driver patch already applied."
    elif git -C "$DRIVER_DIR" apply --check "$DRIVER_PATCH_PATH" >/dev/null 2>&1; then
        echo "Applying AT-SPI driver patch."
        git -C "$DRIVER_DIR" apply "$DRIVER_PATCH_PATH"
    else
        fail "failed to apply AT-SPI driver patch: $DRIVER_PATCH_PATH"
    fi
fi

source_state_if_exists
if is_pid_alive "${DRIVER_PID:-}" && is_pid_alive "${DBUS_DAEMON_PID:-}" && is_pid_alive "${ATSPI_REGISTRY_PID:-}"; then
    echo "Linux Appium environment already running."
    echo "State file: $STATE_FILE"
    echo "Driver URL: ${SELENIUM_REMOTE_URL:-$DRIVER_URL}"
    exit 0
fi

if command -v ss >/dev/null 2>&1; then
    if ss -ltn "sport = :$DRIVER_PORT" | grep -q ":$DRIVER_PORT"; then
        fail "port $DRIVER_PORT is already in use; run scripts/linux-appium/stop.sh first"
    fi
fi

XVFB_PID=""
if ! pgrep -x Xvfb >/dev/null 2>&1; then
    Xvfb "$DISPLAY_VALUE" -screen 0 1920x1080x24 >"$LOG_DIR/xvfb.log" 2>&1 &
    XVFB_PID="$!"
    sleep 1
    is_pid_alive "$XVFB_PID" || fail "failed to start Xvfb; see $LOG_DIR/xvfb.log"
fi

export DISPLAY="$DISPLAY_VALUE"
unset WAYLAND_DISPLAY GNOME_SETUP_DISPLAY
export XDG_SESSION_TYPE=x11

readarray -t dbus_info < <(dbus-daemon --session --fork --print-address=1 --print-pid=1)
[[ "${#dbus_info[@]}" -ge 2 ]] || fail "failed to start dbus-daemon session"
DBUS_SESSION_BUS_ADDRESS="${dbus_info[0]}"
DBUS_DAEMON_PID="${dbus_info[1]}"
export DBUS_SESSION_BUS_ADDRESS

ATSPI_BUS_LAUNCHER="$(find_atspi_bus_launcher)"
ATSPI_REGISTRYD_BIN="$(find_atspi_registryd)"

"$ATSPI_BUS_LAUNCHER" --launch-immediately >"$LOG_DIR/atspi-bus-launcher.log" 2>&1 &
ATSPI_BUS_PID="$!"
sleep 1

"$ATSPI_REGISTRYD_BIN" --use-gnome-session >"$LOG_DIR/atspi-registryd.log" 2>&1 &
ATSPI_REGISTRY_PID="$!"
sleep 1

(
    cd "$DRIVER_DIR"
    FLASK_APP=selenium-webdriver-at-spi.py python3 -m flask run --host "$DRIVER_HOST" --port "$DRIVER_PORT"
) >"$LOG_DIR/driver.log" 2>&1 &
DRIVER_PID="$!"
sleep 2
is_pid_alive "$DRIVER_PID" || fail "failed to start AT-SPI driver; see $LOG_DIR/driver.log"

SELENIUM_REMOTE_URL="$DRIVER_URL"

{
    state_env_kv REPO_ROOT "$REPO_ROOT"
    state_env_kv DISPLAY "$DISPLAY_VALUE"
    state_env_kv XDG_SESSION_TYPE "x11"
    state_env_kv SELENIUM_REMOTE_URL "$SELENIUM_REMOTE_URL"
    state_env_kv DBUS_SESSION_BUS_ADDRESS "$DBUS_SESSION_BUS_ADDRESS"
    state_env_kv DBUS_DAEMON_PID "$DBUS_DAEMON_PID"
    state_env_kv ATSPI_BUS_PID "$ATSPI_BUS_PID"
    state_env_kv ATSPI_REGISTRY_PID "$ATSPI_REGISTRY_PID"
    state_env_kv DRIVER_PID "$DRIVER_PID"
    state_env_kv XVFB_PID "$XVFB_PID"
    state_env_kv DRIVER_DIR "$DRIVER_DIR"
    state_env_kv DRIVER_PORT "$DRIVER_PORT"
    state_env_kv DRIVER_HOST "$DRIVER_HOST"
    state_env_kv LOG_DIR "$LOG_DIR"
} >"$STATE_FILE"

echo "Linux Appium environment started."
echo "State file: $STATE_FILE"
echo "Driver URL: $SELENIUM_REMOTE_URL"
echo "Logs: $LOG_DIR"
