using Avalonia.Platform;
using Avalonia.Win32;

[assembly: ExportWindowingSubsystem(OperatingSystemType.WinNT, 1, "Win32", typeof(Win32Platform), nameof(Win32Platform.Initialize))]
