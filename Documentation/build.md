# Windows

Avalonia requires at least Visual Studio 2019 and .NET Core SDK 3.1 to build on Windows.

###  Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
git submodule update --init
```

###  Open in Visual Studio

Open the `Avalonia.sln` solution in Visual Studio 2019 or newer. The free Visual Studio Community
edition works fine. Run the `Samples\ControlCatalog.Desktop` project to see the sample application.

# Linux/macOS

It's *not* possible to build the *whole* project on Linux/macOS. You can only build the subset targeting .NET Standard and .NET Core (which is, however, sufficient to get UI working on Linux/macOS). If you want to something that involves changing platform-specific APIs you'll need a Windows machine.

MonoDevelop, Xamarin Studio and Visual Studio for Mac aren't capable of properly opening our solution. You can use Rider (at least 2017.2 EAP) or VSCode instead. They will fail to load most of platform specific projects, but you don't need them to run on .NET Core.

###  Install the latest version of .NET Core

Go to https://www.microsoft.com/net/core and follow instructions for your OS. You need SDK (not just "runtime") package.

###  Additional requirements for macOS

The build process needs [Xcode](https://developer.apple.com/xcode/) to build the native library.  Following the install instructions at the [Xcode](https://developer.apple.com/xcode/) website to properly install.

Linux operating systems ship with their own respective package managers however we will use [Homebrew](https://brew.sh/) to manage packages on macOS.  To install follow the instructions [here](https://docs.brew.sh/Installation).

###  Install CastXML (pre Nov 2020)

Avalonia requires [CastXML](https://github.com/CastXML/CastXML) for XML processing during the build process.  The easiest way to install this is via the operating system's package managers, such as below.

On macOS:
```
brew install https://raw.githubusercontent.com/Homebrew/homebrew-core/8a004a91a7fcd3f6620d5b01b6541ff0a640ffba/Formula/castxml.rb
```

On Debian based Linux (Debian, Ubuntu, Mint, etc):
```
sudo apt install castxml
```

On Red Hat based Linux (Fedora, CentOS, RHEL, etc) using `yum` (`dnf` takes same arguments though):
```
sudo yum install castxml
```


###  Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
cd Avalonia
git submodule update --init --recursive
```

### Build native libraries (macOS only)

On macOS it is necessary to build and manually install the respective native libraries using [Xcode](https://developer.apple.com/xcode/). Execute the build script in the root project with the `CompileNative` task. It will build the headers, build the libraries, and place them in the appropriate place to allow .NET to find them at compilation and run time.

```bash
./build.sh CompileNative
```

###  Build and Run Avalonia

```
cd samples/ControlCatalog.NetCore
dotnet restore
dotnet run
```
