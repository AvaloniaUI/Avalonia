using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Defines macOS-specific options for top-level platform implementations.
/// </summary>
[PrivateApi]
public interface IMacOSOptionsTopLevelImpl : ITopLevelImpl
{
    /// <summary>
    /// Sets the top-leading position of the native macOS window buttons.
    /// </summary>
    /// <param name="position">The position, or <see langword="null"/> to use the system layout.</param>
    void SetTrafficLightPosition(Point? position);
}
