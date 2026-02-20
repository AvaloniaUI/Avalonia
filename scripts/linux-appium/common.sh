#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

STATE_DIR="${APPIUM_LINUX_STATE_DIR:-$REPO_ROOT/artifacts/linux-appium}"
STATE_FILE="$STATE_DIR/state.env"
LOG_DIR="$STATE_DIR/logs"

DISPLAY_VALUE="${APPIUM_LINUX_DISPLAY:-:99}"
DRIVER_PORT="${APPIUM_LINUX_PORT:-4723}"
DRIVER_HOST="${APPIUM_LINUX_HOST:-127.0.0.1}"
DRIVER_DIR="${APPIUM_LINUX_DRIVER_DIR:-/tmp/at-spi-driver}"
DRIVER_REPO_URL="${APPIUM_LINUX_DRIVER_REPO_URL:-https://github.com/KDE/selenium-webdriver-at-spi.git}"
DRIVER_URL="http://${DRIVER_HOST}:${DRIVER_PORT}"

TEST_HOST_PATH="${APPIUM_LINUX_TEST_HOST_PATH:-$REPO_ROOT/tests/Avalonia.IntegrationTests.Appium/bin/Debug/net10.0/Avalonia.IntegrationTests.Appium}"
TEST_PROJECT_PATH="${APPIUM_LINUX_TEST_PROJECT_PATH:-$REPO_ROOT/tests/Avalonia.IntegrationTests.Appium/Avalonia.IntegrationTests.Appium.csproj}"
APP_PROJECT_PATH="${APPIUM_LINUX_APP_PROJECT_PATH:-$REPO_ROOT/samples/IntegrationTestApp/IntegrationTestApp.csproj}"

fail() {
    echo "error: $*" >&2
    exit 1
}

require_command() {
    command -v "$1" >/dev/null 2>&1 || fail "required command not found: $1"
}

is_pid_alive() {
    local pid="${1:-}"
    [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null
}

ensure_dirs() {
    mkdir -p "$STATE_DIR" "$LOG_DIR"
}

source_state_if_exists() {
    if [[ -f "$STATE_FILE" ]]; then
        # shellcheck disable=SC1090
        source "$STATE_FILE"
    fi
}

state_env_kv() {
    local key="$1"
    local value="${2:-}"
    printf '%s=%q\n' "$key" "$value"
}

ensure_driver_repo() {
    if [[ -d "$DRIVER_DIR/.git" ]]; then
        return
    fi

    require_command git
    echo "Cloning AT-SPI WebDriver to $DRIVER_DIR"
    rm -rf "$DRIVER_DIR"
    git clone --depth 1 "$DRIVER_REPO_URL" "$DRIVER_DIR" >/dev/null
}

find_atspi_bus_launcher() {
    if [[ -x /usr/libexec/at-spi-bus-launcher ]]; then
        echo /usr/libexec/at-spi-bus-launcher
        return
    fi
    if command -v at-spi-bus-launcher >/dev/null 2>&1; then
        command -v at-spi-bus-launcher
        return
    fi
    fail "at-spi-bus-launcher not found"
}

find_atspi_registryd() {
    if [[ -x /usr/libexec/at-spi2-registryd ]]; then
        echo /usr/libexec/at-spi2-registryd
        return
    fi
    if command -v at-spi2-registryd >/dev/null 2>&1; then
        command -v at-spi2-registryd
        return
    fi
    fail "at-spi2-registryd not found"
}

