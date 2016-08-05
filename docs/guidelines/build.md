# Building Avalonia

## Windows

Avalonia requires at least Visual Studio 2015 to build on Windows.

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
edition works fine.

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

Unless you have a very current version of monodevelop (6.1.x or newer), it is necessary to manually
restore the Nuget depdendencies:

```
cd Avalonia
mkdir -p .nuget
wget -O .nuget/nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
mono .nuget/nuget.exe restore Avalonia.sln
```

### Build Avalonia

Build avalonia with `xbuild`:

```
xbuild /p:Configuration=Release Avalonia.travis-mono.sln
```

### Open Avalonia in MonoDevelop

Start MonoDevelop and open the `Avalonia.sln` solution. Set the Samples/TestApplication
project as the startup project and click Run.

There will be some compile errors in tests for the Windows platform, which can be safely
ignored.

Enjoy playing with Avalonia! You may want to explore some of the other Samples for a
flavor of the Platform
