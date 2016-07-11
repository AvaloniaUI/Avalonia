# Building Avalonia 

## Windows

Avalonia requires Visual Studio 2015 to build on Windows.

### Install GTK Sharp

For the moment under windows, you must have [gtk-sharp](http://www.mono-project.com/download/#download-win) installed. Note that after installing the package your machine may require a restart before GTK# is added to your path. We hope to remove or make this dependency optional at some point in the future.

### Clone the Avalonia repository

    git clone https://github.com/AvaloniaUI/Avalonia.git

We currently need to build our own private version of some libraries. These are linked as submodules in the git repository, so run:

    git submodule update --init
    
## Linux

This guide Written for Ubuntu 15.04 - I'm not sure how well it applies to other distributions, but
please submit a PR if you have anything to add.

### Install Latest Mono

That the time of writing, mono 4.2 aplha was needed to build. Add mono package sources by following
instructions below for the stable channel and then add the alpha channel as well.

http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives

Then install the needed packages:

    sudo apt-get install git mono-devel referenceassemblies-pcl monodevelop

### Clone the Avalonia repository

    git clone https://github.com/AvaloniaUI/Avalonia.git

We currently need to build our own private version of ReactiveUI as it doesn't work on mono. This
is linked as a submodule in the git repository, so run:

    git submodule update --init
    
The next step is to download the Skia native libraries. Run ```getnatives.sh``` script which can be found under the folder ```src\Skia\```.
   
### Load the Project in MonoDevelop

Start MonoDevelop and open the `Avalonia.sln` solution. Wait for MonoDevelop to install the
project's NuGet packages.

Set the TestApplication project as the startup project and click Run.

There will be some compile errors in the tests, but ignore them for now. 

You can track the Linux version's progress in the [Linux issue](https://github.com/AvaloniaUI/Avalonia/issues/78).
