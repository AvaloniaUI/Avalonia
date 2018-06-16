using System.Reflection;
using Avalonia.Gtk3;
using Avalonia.Platform;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: ExportWindowingSubsystem(OperatingSystemType.WinNT, 2, "GTK3", typeof(Gtk3Platform), nameof(Gtk3Platform.Initialize))]
[assembly: ExportWindowingSubsystem(OperatingSystemType.Linux, 1, "GTK3", typeof(Gtk3Platform), nameof(Gtk3Platform.Initialize))]
[assembly: ExportWindowingSubsystem(OperatingSystemType.OSX, 2, "GTK3", typeof(Gtk3Platform), nameof(Gtk3Platform.Initialize))]