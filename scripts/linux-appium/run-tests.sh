#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck disable=SC1091
source "$SCRIPT_DIR/common.sh"

build=true
output_verbosity="${APPIUM_LINUX_TEST_OUTPUT:-Normal}"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --no-build)
            build=false
            shift
            ;;
        --output)
            [[ $# -ge 2 ]] || fail "--output requires a value"
            output_verbosity="$2"
            shift 2
            ;;
        --help|-h)
            cat <<'EOF'
Usage:
  scripts/linux-appium/run-tests.sh [--no-build] [--output Normal|Detailed] [xunit test-host args...]

Examples:
  scripts/linux-appium/run-tests.sh
  scripts/linux-appium/run-tests.sh -- --filter-method "*ButtonWithAcceleratorKey"
  scripts/linux-appium/run-tests.sh --output Detailed --filter-method "*Linux_Can_Change_*"
EOF
            exit 0
            ;;
        --)
            shift
            break
            ;;
        *)
            break
            ;;
    esac
done

if [[ ! -f "$STATE_FILE" ]]; then
    "$SCRIPT_DIR/start.sh"
fi

source_state_if_exists
is_pid_alive "${DRIVER_PID:-}" || "$SCRIPT_DIR/start.sh"
source_state_if_exists

if [[ "$build" == true ]]; then
    dotnet build "$APP_PROJECT_PATH" -v minimal
    dotnet build "$TEST_PROJECT_PATH" -v minimal
fi

[[ -x "$TEST_HOST_PATH" ]] || fail "test host not found: $TEST_HOST_PATH (build the test project first)"

export DISPLAY="${DISPLAY:-$DISPLAY_VALUE}"
export XDG_SESSION_TYPE="${XDG_SESSION_TYPE:-x11}"
export DBUS_SESSION_BUS_ADDRESS
export SELENIUM_REMOTE_URL
unset WAYLAND_DISPLAY GNOME_SETUP_DISPLAY

cd "$REPO_ROOT"

cmd=("$TEST_HOST_PATH" "--no-progress" "--output" "$output_verbosity")
if [[ $# -gt 0 ]]; then
    cmd+=("$@")
fi

echo "Running: ${cmd[*]}"
"${cmd[@]}"

