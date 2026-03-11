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
        /// Set by the render loop. The timer calls this on each tick.
        /// </summary>
        /// <remarks>
        /// This can be called on any thread; it is the responsibility of the subscriber to
        /// switch execution to the right thread.
        /// </remarks>
        Action<TimeSpan>? Tick { get; set; }

        /// <summary>
        /// Indicates if the timer ticks on a non-UI thread.
        /// </summary>
        bool RunsInBackground { get; }

        /// <summary>
        /// Starts the timer. Called by the render loop under a lock.
        /// Will not be called if the timer is already started.
        /// May be called from any thread.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the timer. Called by the render loop under a lock.
        /// Will not be called if the timer is already stopped.
        /// Typically called from the timer's own tick thread (within the tick callback).
        /// </summary>
        void Stop();
    }
}
