// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Gtk;
using Avalonia.Platform;
using System.Reflection;

// Information about this assembly is defined by the following attributes.
// Change them to the values specific to your project.
[assembly: AssemblyTitle("Avalonia.Gtk")]

[assembly: ExportWindowingSubsystem(OperatingSystemType.WinNT, 3, "GTK", typeof(GtkPlatform), nameof(GtkPlatform.Initialize))]
[assembly: ExportWindowingSubsystem(OperatingSystemType.Linux, 2, "GTK", typeof(GtkPlatform), nameof(GtkPlatform.Initialize))]
[assembly: ExportWindowingSubsystem(OperatingSystemType.OSX, 3, "GTK", typeof(GtkPlatform), nameof(GtkPlatform.Initialize))]

