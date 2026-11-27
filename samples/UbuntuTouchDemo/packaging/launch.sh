#!/bin/sh
set -eu
cd "${0%/*}"
export DOTNET_EnableDiagnostics=0
export RESOURCE_NAME="${APP_ID:-avalonia.inputdemo_inputdemo}"
unset DISPLAY
exec ./app/UbuntuTouchDemo --wayland "$@"
