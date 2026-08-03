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
        /// Gets or sets the callback to be invoked when the timer ticks.
        /// This property can be set from any thread, but it's guaranteed that it's not set concurrently
        /// (i. e. render loop always does it under a lock).
        /// Setting the value to null suggests the timer to stop ticking, however
        /// timer is allowed to produce ticks on the previously set value as long as it stops doing so
        /// </summary>
        /// <remarks>
        /// The callback can be invoked on any thread
        /// </remarks>
        Action<TimeSpan>? Tick { get; set; }

        /// <summary>
        /// Indicates if the timer ticks on a non-UI thread.
        /// </summary>
        bool RunsInBackground { get; }
    }
}
