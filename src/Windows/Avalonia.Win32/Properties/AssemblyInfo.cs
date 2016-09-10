// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;
using Avalonia.Win32;
using System.Reflection;

[assembly: AssemblyTitle("Avalonia.Win32")]
[assembly: ExportWindowingSubsystem(OperatingSystemType.WinNT, 1, "Win32", typeof(Win32Platform), nameof(Win32Platform.Initialize))]
