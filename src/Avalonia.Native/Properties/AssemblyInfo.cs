using Avalonia.Native;
using Avalonia.Platform;

[assembly: ExportWindowingSubsystem(OperatingSystemType.OSX, 1, "AvaloniaNative", typeof(AvaloniaNativePlatform), nameof(AvaloniaNativePlatform.Initialize))]
