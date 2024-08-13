# Running Integration Tests

## Windows

### Prerequisites

- Install WinAppDriver: https://github.com/microsoft/WinAppDriver

### Running

- Run WinAppDriver (it gets installed to the start menu)
- Run the tests in this project

## macOS

### Prerequisites

- Install Appium: https://appium.io/
- Give [Xcode helper the required permissions](https://apple.stackexchange.com/questions/334008)
- `cd samples/IntegrationTestApp` then `./bundle.sh` to create an app bundle for `IntegrationTestApp`
- Register the app bundle by running `open -n ./bin/Debug/net8.0/osx-arm64/publish/IntegrationTestApp.app` 

### Running

- Run `appium`
- Run the tests in this project

Each time you make a change to Avalonia or `IntegrationTestApp`, re-run the `bundle.sh` script (registration only needs to be done once).

### Appium 2

Tests in this project are configured to run with Appium 1 (as only this version supports WinAppDriver).
If you need to run with Appium 2 on macOS, extra steps are required:
- Install Appium 2 with [mac2 driver](https://github.com/appium/appium-mac2-driver)
- Set `<IsRunningAppium2>true</IsRunningAppium2>` msbuild property on the test project or globally
- Run appium 2 with `appium --base-path=/wd/hub` (custom base path is required)
- Run tests as normally
