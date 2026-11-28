using System;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform;

namespace Avalonia.Controls.Platform;

/// <summary>
/// Defines macOS-specific features of a top-level platform implementation.
/// </summary>
[PrivateApi]
public interface IMacOSOptionsTopLevelImpl : ITopLevelImpl
{
    /// <summary>
    /// Gets the size of the native window button group, in device-independent pixels.
    /// </summary>
    /// <remarks>
    /// The size is zero while the buttons can't be placed in the client area, for example when the client area
    /// isn't extended, when the window isn't fully decorated, or while the window is in native full-screen mode.
    /// </remarks>
    Size TrafficLightSize { get; }

    /// <summary>
    /// Gets or sets a method called when <see cref="TrafficLightSize"/> changes.
    /// </summary>
    Action<Size>? TrafficLightSizeChanged { get; set; }

    /// <summary>
    /// Gets the direction in which the native window buttons are laid out.
    /// </summary>
    /// <remarks>
    /// The direction comes from the native title bar and can differ from the flow direction of the window content.
    /// </remarks>
    FlowDirection TitlebarFlowDirection { get; }

    /// <summary>
    /// Moves the native window buttons to the specified position.
    /// </summary>
    /// <param name="position">
    /// The top-leading corner of the button group, in device-independent pixels from the top-leading corner of
    /// the window, or <see langword="null"/> to restore the system layout.
    /// </param>
    /// <remarks>
    /// The position takes effect only while <see cref="TrafficLightSize"/> is nonzero.
    /// Until then, the buttons use the system layout.
    /// </remarks>
    void SetTrafficLightPosition(Point? position);
}
