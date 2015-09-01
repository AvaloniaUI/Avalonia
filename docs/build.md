# Building Perspex 

## Windows

Perspex requires Visual Studio 2015 to build on Windows.

### Install GTK Sharp

To compile the full project under windows, you must have gtk-sharp installed. However, if you're 
not interested in building the cross-platform bits you can simply unload the Perspex.Cairo and 
Perspex.Gtk project in Visual Studio.

### Clone the Perspex repository

    git clone https://github.com/grokys/Perspex.git

We currently need to build our own private version of ReactiveUI as it doesn't work on mono. This
is linked as a submodule in the git repository, so run:

    git submodule update --init

## Linux

This guide Written for Ubuntu 15.04 - I'm not sure how well it applies to other distributions, but
please submit a PR if you have anything to add.

### Install Latest Mono

That the time of writing, mono 4.2 aplha was needed to build. Add mono package sources by following
instructions below for the stable channel, and then add the alpha channel to 
`/etc/apt/sources.list.d/mono-xamarin-alpha.list` as well.

http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives

Then install the needed packages:

    sudo apt-get install git mono-devel referenceassemblies-pcl monodevelop

### Clone the Perspex repository

    git clone https://github.com/grokys/Perspex.git

We currently need to build our own private version of ReactiveUI as it doesn't work on mono. This
is linked as a submodule in the git repository, so run:

    git submodule update --init
   
### Load the Project in MonoDevelop

Start MonoDevelop and open the `Perspex.sln` solution. Wait for MonoDevelop to install the
project's NuGet packages.

Set the TestApplication project as the startup project and click Run.

There will be some compile errors in the tests, but ignore them for now. 

You can track the Linux version's progress in the [Linux issue](https://github.com/grokys/Perspex/issues/78).
