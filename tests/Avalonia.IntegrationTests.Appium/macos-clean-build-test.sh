# Cleans, builds, and runs integration tests on macOS.
# Can be used by `git bisect run` to automatically find the commit which introduced a problem. 
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
cd "$SCRIPT_DIR"/../.. || exit
git clean -xdf
./build.sh CompileNative
./samples/IntegrationTestApp/bundle.sh
dotnet test tests/Avalonia.IntegrationTests.Appium/ -l "console;verbosity=detailed"
