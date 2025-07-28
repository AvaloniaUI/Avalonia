namespace Avalonia.Input;

/// <summary>
/// Represents the kind of a <see cref="DataFormat"/>.
/// </summary>
public enum DataFormatKind
{
    /// <summary>
    /// The data format is specific to the application.
    /// </summary>
    Application,

    /// <summary>
    /// The data format is specific to the current operating system.
    /// </summary>
    OperatingSystem,

    /// <summary>
    /// The data format is cross-platform.
    /// </summary>
    CrossPlatform
}
