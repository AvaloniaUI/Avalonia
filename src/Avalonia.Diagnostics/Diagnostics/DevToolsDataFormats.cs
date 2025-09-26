using System;
using Avalonia.Input;

namespace Avalonia.Diagnostics;

/// <summary>
/// Contains data formats related to dev tools.
/// </summary>
public static class DevToolsDataFormats
{
    // TODO: this name isn't ideal. For instance, it's not a valid UTI for macOS.
    // We currently have a converter in place in native code for backwards compatibility with IDataObject,
    // but this should ideally be removed at some point.
    // Consider using DataFormat.CreateApplicationFormat() instead (breaking change).

    /// <summary>
    /// Gets the clipboard data format representing a selector.
    /// It's used for quick format recognition in IDEs.
    /// </summary>
    public static DataFormat<string> Selector { get; } = DataFormat.CreateStringPlatformFormat("Avalonia_DevTools_Selector");
}
