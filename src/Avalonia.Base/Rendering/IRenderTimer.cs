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
        /// Set by the render loop to control ticking.
        /// Setting a non-null value starts the timer; setting null stops it.
        /// Called under a lock by the render loop — will not be set concurrently.
        /// </summary>
        /// <remarks>
        /// The callback can be invoked on any thread; it is the responsibility of the subscriber to
        /// switch execution to the right thread.
        /// </remarks>
        Action<TimeSpan>? Tick { get; set; }

        /// <summary>
        /// Indicates if the timer ticks on a non-UI thread.
        /// </summary>
        bool RunsInBackground { get; }
    }
}
