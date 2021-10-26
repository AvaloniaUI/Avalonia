#!/usr/bin/env bash

SCRIPT_DIR="$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
SDK_DIR=$SCRIPT_DIR/sdk

DOTNET_INSTALL_FILE="$SDK_DIR/dotnet-install.sh"
DOTNET_INSTALL_URL="https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.sh"

DOTNET_VERSION=5.0.402

mkdir -p "$SDK_DIR"

if [ ! -f "$DOTNET_INSTALL_FILE" ]; then
	curl -Lsfo "$DOTNET_INSTALL_FILE" "$DOTNET_INSTALL_URL"
	chmod +x "$DOTNET_INSTALL_FILE"
fi

"$DOTNET_INSTALL_FILE" --install-dir "$SDK_DIR" --version 5.0.402
"$DOTNET_INSTALL_FILE" --install-dir "$SDK_DIR" --version 3.1.20 --runtime dotnet
"$DOTNET_INSTALL_FILE" --install-dir "$SDK_DIR" --version 3.1.20 --runtime aspnetcore


