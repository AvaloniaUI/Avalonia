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
        /// <remarks>
        /// This event can be raised on any thread; it is the responsibility of the subscriber to
        /// switch execution to the right thread.
        /// </remarks>
        event EventHandler<EventArgs> Tick;
    }
}