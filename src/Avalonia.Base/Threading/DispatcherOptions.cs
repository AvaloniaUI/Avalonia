using System;

namespace Avalonia.Threading;

/// <summary>
/// AppBuilder options for configuring the <see cref="Dispatcher"/>.
/// </summary>
public class DispatcherOptions
{
    /// <summary>
    /// Gets or sets a timeout after which the dispatcher will start prioritizing input events over
    /// rendering. The default value is 1 second.
    /// </summary>
    /// <remarks>
    /// If no input events are processed within this time, the dispatcher will start prioritizing
    /// input events over rendering to prevent the application from becoming unresponsive. This may
    /// need to be lowered on resource-constrained platforms where input events are processed on
    /// the same thread as rendering.
    /// </remarks>
    public TimeSpan InputStarvationTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets a value indicating whether rendering should be performed synchronously.<br/>
    /// Otherwise, rendering will be invoked from UI thread to the render thread, back to the UI thread and then back to the render thread.
    /// </summary>
    public bool InstantRendering { get; set; } = false;
}
