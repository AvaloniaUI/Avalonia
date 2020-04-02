#!/usr/bin/env bash

echo $(bash --version 2>&1 | head -n 1)

#CUSTOMPARAM=0
BUILD_ARGUMENTS=()
for i in "$@"; do
    case $(echo $1 | awk '{print tolower($0)}') in
        # -custom-param) CUSTOMPARAM=1;;
        *) BUILD_ARGUMENTS+=("$1") ;;
    esac
    shift
done

set -eo pipefail
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

###########################################################################
# CONFIGURATION
###########################################################################

BUILD_PROJECT_FILE="$SCRIPT_DIR/nukebuild/_build.csproj"
TEMP_DIRECTORY="$SCRIPT_DIR//.tmp"

DOTNET_GLOBAL_FILE="$SCRIPT_DIR//global.json"
DOTNET_INSTALL_URL="https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.sh"
DOTNET_CHANNEL="Current"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export NUGET_XMLDOC_MODE="skip"

###########################################################################
# EXECUTION
###########################################################################

function FirstJsonValue {
    perl -nle 'print $1 if m{"'$1'": "([^"\-]+)",?}' <<< ${@:2}
}

# If global.json exists, load expected version
if [ -f "$DOTNET_GLOBAL_FILE" ]; then
    DOTNET_VERSION=$(FirstJsonValue "version" $(cat "$DOTNET_GLOBAL_FILE"))
    if [ "$DOTNET_VERSION" == ""  ]; then
        unset DOTNET_VERSION
    fi
fi

# If dotnet is installed locally, and expected version is not set or installation matches the expected version
if [[ -x "$(command -v dotnet)" && (-z ${DOTNET_VERSION+x} || $(dotnet --version) == "$DOTNET_VERSION") ]]; then
    export DOTNET_EXE="$(command -v dotnet)"
else
    DOTNET_DIRECTORY="$TEMP_DIRECTORY/dotnet-unix"
    export DOTNET_EXE="$DOTNET_DIRECTORY/dotnet"
    
    # Download install script
    DOTNET_INSTALL_FILE="$TEMP_DIRECTORY/dotnet-install.sh"
    mkdir -p "$TEMP_DIRECTORY"
    curl -Lsfo "$DOTNET_INSTALL_FILE" "$DOTNET_INSTALL_URL"
    chmod +x "$DOTNET_INSTALL_FILE"
    
    # Install by channel or version
    if [ -z ${DOTNET_VERSION+x} ]; then
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --channel "$DOTNET_CHANNEL" --no-path
    else
        "$DOTNET_INSTALL_FILE" --install-dir "$DOTNET_DIRECTORY" --version "$DOTNET_VERSION" --no-path
    fi
fi

echo "Microsoft (R) .NET Core SDK version $("$DOTNET_EXE" --version)"

"$DOTNET_EXE" run --project "$BUILD_PROJECT_FILE" -- ${BUILD_ARGUMENTS[@]}
