using Avalonia.Metadata;
using Avalonia.Platform.Storage;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Factory allows to register custom ILauncher.
/// Can be used for overriding some URL handlers for example
/// </summary>
[Unstable]
public interface ILauncherFactory
{
    ILauncher CreateLauncher(TopLevel topLevel);
}
