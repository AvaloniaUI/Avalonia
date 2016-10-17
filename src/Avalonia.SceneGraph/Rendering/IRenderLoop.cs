using System;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface implemented by an application render loop.
    /// </summary>
    public interface IRenderLoop
    {
        /// <summary>
        /// Raised when the render loop ticks to signal a new frame should be drawn.
        /// </summary>
        event EventHandler<EventArgs> Tick;
    }
}