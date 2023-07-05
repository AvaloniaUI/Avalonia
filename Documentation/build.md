# Windows

Avalonia requires at least Visual Studio 2022 and dotnet 7-rc2 SDK 7.0.100-rc.2 to build on all platforms.

##  Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
cd Avalonia
git submodule update --init
```

## Install the required version of the .NET Core SDK

Go to https://dotnet.microsoft.com/download/visual-studio-sdks and install the latest version of the .NET Core SDK compatible with Avalonia UI. Make sure to download the SDK (not just the "runtime") package. The version compatible is indicated within the [global.json](https://github.com/AvaloniaUI/Avalonia/blob/master/global.json) file. Note that Avalonia UI does not always use the latest version and is hardcoded to use the last version known to be compatible (SDK releases may break the builds from time-to-time).

### Installing necessary .NET Workloads

.NET SDK requires developers to install workloads for each platform they are targeting.
Since Avalonia targets pretty much every supported .NET platform, you need to install these workloads as well. 
Running it from the command line:
```
dotnet workload install android ios wasm-tools wasm-experimental
```

macOS workloads are not required to build Avalonia.
Note: on Unix OS you need to run this command from sudo.

##  Build and Run Avalonia

```
cd samples\ControlCatalog.NetCore
dotnet restore
dotnet run
```

##  Opening in Visual Studio

If you want to open Avalonia in Visual Studio you have two options:

- Avalonia.sln: This contains the whole of Avalonia in including desktop, mobile and web. You must have a number of dotnet workloads installed in order to build everything in this solution
- Avalonia.Desktop.slnf: This solution filter opens only the parts of Avalonia required to run on desktop. This requires no extra workloads to be installed.

Avalonia requires Visual Studio 2022 or newer. The free Visual Studio Community edition works fine.

Build and run `ControlCatalog.NetCore` project to see the sample application.

### Visual Studio Troubleshooting

 * **Error CS0006: Avalonia.DesktopRuntime.dll could not be found**

    It is common for the first build to fail with the errors below (also discussed in [#4257](https://github.com/AvaloniaUI/Avalonia/issues/4257)).
    ```
    >CSC : error CS0006: Metadata file 'C:\...\Avalonia\src\Avalonia.DesktopRuntime\bin\Debug\netcoreapp2.0\Avalonia.DesktopRuntime.dll' could not be found
    >CSC : error CS0006: Metadata file 'C:\...\Avalonia\packages\Avalonia\bin\Debug\netcoreapp2.0\Avalonia.dll' could not be found
    ```
    To correct this, right click on the `Avalonia.DesktopRuntime` project then press `Build` to build the project manually. Afterwards the solution should build normally and the ControlCatalog can be run.
 * **Error MSB4062 GenerateAvaloniaResourcesTask**

    Same as previous one, you need to manually build `Avalonia.Build.Tasks` project at least once.

    Alternatively, you can build the solution once with Nuke.

## Building packages with Nuke

Install Nuke
`dotnet tool install --global Nuke.GlobalTool --version 6.2.1`

Build project:
`nuke --target Compile --configuration Release`

And run tests:
`nuke --target RunTests --configuration Release`

Or if you need to create nuget packages as well (it will compile and run tests automatically):
`nuke --target Package --configuration Release`

# Linux/macOS

It's *not* possible to build the *whole* project on Linux/macOS. You can only build the subset targeting .NET Standard and .NET Core (which is, however, sufficient to get UI working on Linux/macOS). If you want to something that involves changing platform-specific APIs you'll need a Windows machine.

MonoDevelop, Xamarin Studio and Visual Studio for Mac aren't capable of properly opening our solution. You can use Rider (at least 2017.2 EAP) or VSCode instead. They will fail to load most of platform specific projects, but you don't need them to run on .NET Core.

##  Install the latest version of the .NET Core SDK

Go to https://www.microsoft.com/net/core and follow the instructions for your OS. Make sure to download the SDK (not just the "runtime") package.

##  Additional requirements for macOS

The build process needs [Xcode](https://developer.apple.com/xcode/) to build the native library.  Following the install instructions at the [Xcode](https://developer.apple.com/xcode/) website to properly install.

##  Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
cd Avalonia
git submodule update --init --recursive
```

## Build native libraries (macOS only)

On macOS it is necessary to build and manually install the respective native libraries using [Xcode](https://developer.apple.com/xcode/). Execute the build script in the root project with the `CompileNative` task. It will build the headers, build the libraries, and place them in the appropriate place to allow .NET to find them at compilation and run time.

```bash
./build.sh CompileNative
```

##  Build and Run Avalonia

```
cd samples/ControlCatalog.NetCore
dotnet restore
dotnet run
```
