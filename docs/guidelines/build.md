# Building Avalonia

## Windows

Avalonia requires at least Visual Studio 2017 to build on Windows.

### Install GTK Sharp

For the moment under windows, you must have [gtk-sharp](http://www.mono-project.com/download/#download-win)
installed. Note that after installing the package your machine may require a restart before GTK# is
added to your path. We hope to remove or make this dependency optional at some point in the future.

### Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
git submodule update --init
```

### Open in Visual Studio

Open the `Avalonia.sln` solution in Visual Studio 2015 or newer. The free Visual Studio Community
edition works fine. Run the `Samples\ControlCatalog.Desktop` project to see the sample application.

## Linux

### Install the latest version of Mono

To build Avalonia under Linux, you need to have a recent version of Mono installed. Mono is a cross-
platform, open source .Net platform. There is a very good chance that the version of Mono that came
with your Linux distribution is too old, so you want to install a more up-to-date version. The most
convenient way to to this is through your package manager. The Mono project has great [installation
instructions for many popular Linux distros](http://www.mono-project.com/docs/getting-started/install/linux).

This will make the most up-to-date Mono release available through your package manager, and offer
you updates as they become available.

Once you have your package manager configured for the Mono repository, install the `mono-devel`
package, for example on ubuntu:

```
sudo apt-get install mono-devel
```

Once installed, check the version of mono to ensure it's at least 4.4.2:

```
mono --version
```

### Clone the Avalonia repository

```
git clone https://github.com/AvaloniaUI/Avalonia.git
git submodule update --init
```

### Restore nuget packages

```
cd Avalonia
mkdir -p .nuget
wget -O .nuget/nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
mono .nuget/nuget.exe restore Avalonia.sln
```

### Build and Run Avalonia

To build Avalonia in the `Debug` configuration:

```
xbuild /p:Platform=Mono /p:Configuration=Debug Avalonia.sln
```

You should now be able to run the ControlCatalog.Desktop sample:

```
mono ./samples/ControlCatalog.Desktop/bin/Debug/ControlCatalog.Desktop.exe
```

### Building Avalonia in MonoDevelop

Flatpak version will *NOT* work. Version from https://github.com/cra0zy/monodevelop-run-installer/ might work if you are very lucky. Make sure that you have the latest version of Mono (from alpha update channel) and .NET Core SDK. Make sure to follow `FrameworkPathOverride` workaround from https://github.com/dotnet/sdk/issues/335

### Building and running Avalonia in Rider

For Linux/OSX you'll probably need to apply workaround from https://github.com/dotnet/sdk/issues/335

Just add `export FrameworkPathOverride=/usr/lib/mono/4.6.1-api` (or `export FrameworkPathOverride=/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.6.1-api` for OSX)

