using Avalonia.Platform;
using Avalonia.Skia;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Avalonia.Skia.Desktop")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Avalonia.Skia.Desktop")]
[assembly: AssemblyCopyright("Copyright ©  2015")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("925dd807-b651-475f-9f7c-cbeb974ce43d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: ExportRenderingSubsystem(OperatingSystemType.WinNT, 2, "Skia", typeof(SkiaPlatform), nameof(SkiaPlatform.Initialize))]
[assembly: ExportRenderingSubsystem(OperatingSystemType.OSX, 1, "Skia", typeof(SkiaPlatform), nameof(SkiaPlatform.Initialize))]
[assembly: ExportRenderingSubsystem(OperatingSystemType.Linux, 1, "Skia", typeof(SkiaPlatform), nameof(SkiaPlatform.Initialize))]