#/bin/bash
set -e
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
cd $SCRIPT_DIR/../..
dotnet run --project ./nukebuild/_build.csproj --target GenerateCppHeaders

