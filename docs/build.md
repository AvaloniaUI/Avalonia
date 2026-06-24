##  Clone the Avalonia repository

```bash
git clone --recurse-submodules https://github.com/AvaloniaUI/Avalonia.git
```

## Install the required version of the .NET Core SDK

Go to https://dotnet.microsoft.com/en-us/download/visual-studio-sdks and install the latest version of the .NET SDK compatible with Avalonia. Make sure to download the SDK (not just the "runtime") package. The version compatible is indicated within the [global.json](https://github.com/AvaloniaUI/Avalonia/blob/master/global.json) file. Note that Avalonia does not always use the latest version and is hardcoded to use the last version known to be compatible (SDK releases may break the builds from time-to-time).

### Install necessary .NET Workloads

.NET SDK requires developers to install workloads for each platform they are targeting.
Since Avalonia targets pretty much every supported .NET platform, you need to install these workloads as well. 
Running it from the command line:
```bash
dotnet workload install android ios tvos maccatalyst wasm-tools
```

macOS workloads are not required to build Avalonia.
Note: on Unix OS you need to run this command from sudo.

## Build and Run Avalonia

```bash
cd samples\ControlCatalog.Desktop
dotnet restore
dotnet run
```

## IDEs

Visual Studio, Visual Studio Code and Rider and supported.  
You need a version that support at least .NET 10 (e.g. Visual Studio 2026 or Rider 2025.3).

If you want to open Avalonia in your preferred IDE, you have two options:

- `Avalonia.slnx`: This contains the whole of Avalonia in including desktop, mobile and web. You must have a number of dotnet workloads installed in order to build everything in this solution
- `Avalonia.Desktop.slnf`: This solution filter opens only the parts of Avalonia required to run on desktop. This requires no extra workloads to be installed.

Build and run the `ControlCatalog.Desktop` project to see the sample application.

### Troubleshooting

#### Submodules

When updating your local repository, always ensure that the submodules are up-to-date to avoid any problems:

```bash
git submodule update --init --recursive
```

#### Error MSB4062 GenerateAvaloniaResourcesTask

If you encounter this error when building inside the IDE, manually build the `Avalonia.Build.Tasks` project at least once.  
Alternatively, you can build the solution once with Nuke.

## Building packages with Nuke

Install Nuke:
```bash
dotnet tool install --global Nuke.GlobalTool
```

Build project:
```bash
nuke --target Compile --configuration Release
```

And run tests:
```bash
nuke --target RunTests --configuration Release
```

Or if you need to create nuget packages as well (it will compile and run tests automatically):
```bash
nuke --target Package --configuration Release
```

Alternatively, you can run nuke build directly without installing Nuke global tool. Replace `nuke` with either `./build.sh` (macOS/Linux) or `.\build.ps1` (Windows).  
Examples:

On Windows:

```bash
.\build.ps1 --configuration Debug
```

On macOS and Linux:

```bash
./build.sh --configuration Debug
```

For integration tests, see [readme](https://github.com/AvaloniaUI/Avalonia/tree/master/tests/Avalonia.IntegrationTests.Appium#readme) next to the project.

## Native libraries (macOS only)

On macOS, the build process needs [Xcode](https://developer.apple.com/xcode/) to build the native library. Follow the installation instructions on the [Xcode](https://developer.apple.com/xcode/) website to install it.

Then, build and manually install the corresponding native libraries using Xcode. Execute the build script in the root project with the `CompileNative` task. It will build the headers and libraries, and place them in the appropriate locations so .NET can find them at compile and run time.

```bash
./build.sh CompileNative
```

## Building Avalonia into a local NuGet cache

See [Building Local NuGet Packages](nuget.md)
