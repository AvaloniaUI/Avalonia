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

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export NUGET_XMLDOC_MODE="skip"

dotnet --info

dotnet run --project "$BUILD_PROJECT_FILE" -- ${BUILD_ARGUMENTS[@]}
