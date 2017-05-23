// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Cairo;
using Avalonia.Platform;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Avalonia.Cairo")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Avalonia.Cairo")]
[assembly: AssemblyCopyright("Copyright \u00A9  2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("f999ba8b-64e7-40cc-98a4-003f1852d2a3")]

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

[assembly: ExportRenderingSubsystem(OperatingSystemType.WinNT, 3, "Cairo", typeof(CairoPlatform), nameof(CairoPlatform.Initialize), RequiresWindowingSubsystem = "GTK")]
[assembly: ExportRenderingSubsystem(OperatingSystemType.Linux, 2, "Cairo", typeof(CairoPlatform), nameof(CairoPlatform.Initialize), RequiresWindowingSubsystem = "GTK")]
[assembly: ExportRenderingSubsystem(OperatingSystemType.OSX, 3, "Cairo", typeof(CairoPlatform), nameof(CairoPlatform.Initialize), RequiresWindowingSubsystem = "GTK")]
