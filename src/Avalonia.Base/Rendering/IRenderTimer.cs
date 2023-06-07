using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines the interface implemented by an application render timer.
    /// </summary>
    [PrivateApi]
    public interface IRenderTimer
    {
        /// <summary>
        /// Raised when the render timer ticks to signal a new frame should be drawn.
        /// </summary>
        /// <remarks>
        /// This event can be raised on any thread; it is the responsibility of the subscriber to
        /// switch execution to the right thread.
        /// </remarks>
        event Action<TimeSpan> Tick;

        /// <summary>
        /// Indicates if the timer ticks on a non-UI thread
        /// </summary>
        bool RunsInBackground { get; }
    }
}
