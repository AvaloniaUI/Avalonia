# Cleans, builds, and runs integration tests on macOS.
# Can be used by `git bisect run` to automatically find the commit which introduced a problem. 
SCRIPT_DIR=$(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
cd "$SCRIPT_DIR"/../.. || exit
git clean -xdf
pkill node
appium &
pkill IntegrationTestApp
./build.sh CompileNative
rm -rf $(osascript -e "POSIX path of (path to application id \"net.avaloniaui.avalonia.integrationtestapp\")")
pkill IntegrationTestApp
./samples/IntegrationTestApp/bundle.sh
open -n ./samples/IntegrationTestApp/bin/Debug/net7.0/osx-arm64/publish/IntegrationTestApp.app
pkill IntegrationTestApp
open -b net.avaloniaui.avalonia.integrationtestapp
dotnet test tests/Avalonia.IntegrationTests.Appium/ -l "console;verbosity=detailed"
pkill IntegrationTestApp
pkill node
