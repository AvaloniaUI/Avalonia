Compiling Perspex on Ubuntu
---------------------------

Written for Ubuntu 15.04.

Install Latest Mono
-------------------

Add mono package sources by following instructions at:

http://www.mono-project.com/docs/getting-started/install/linux/#debian-ubuntu-and-derivatives

Then install the needed packages:

    sudo apt-get git mono-devel referenceassemblies-pcl monodevelop

Clone the Perspex repository
----------------------------

   git clone https://github.com/grokys/Perspex.git
   
Load the Project in MonoDevelop
-------------------------------

Start MonoDevelop and open the `Perspex-Mono.sln` solution. Wait for MonoDevelop to install the
project's NuGet packages.

Set the TestApplication-Mono project as the startup project and click Run.