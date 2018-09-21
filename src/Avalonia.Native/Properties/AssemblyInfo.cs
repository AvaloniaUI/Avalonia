using Avalonia.MonoMac;
using Avalonia.Platform;

[assembly: ExportWindowingSubsystem(OperatingSystemType.OSX, 1, "MonoMac", typeof(MonoMacPlatform), nameof(MonoMacPlatform.Initialize))]
