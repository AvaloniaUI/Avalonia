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
- Register the app bundle by running `open -n ./bin/Debug/net7.0/osx-arm64/publish/IntegrationTestApp.app` 

### Running

- Run `appium`
- Run the tests in this project

Each time you make a change to Avalonia or `IntegrationTestApp`, re-run the `bundle.sh` script (registration only needs to be done once).


