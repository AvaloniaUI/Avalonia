using System;

namespace Avalonia.Layout
{
    /// <summary>
    /// Provides data for the <see cref="Layoutable.EffectiveViewportChanged"/> event.
    /// </summary>
    public class EffectiveViewportChangedEventArgs : EventArgs
    {
        // Simple pool for reusing event args to reduce allocations during layout passes
        [ThreadStatic]
        private static EffectiveViewportChangedEventArgs? t_pooled;

        public EffectiveViewportChangedEventArgs(Rect effectiveViewport)
        {
            EffectiveViewport = effectiveViewport;
        }

        /// <summary>
        /// Gets the <see cref="Rect"/> representing the effective viewport.
        /// </summary>
        /// <remarks>
        /// The viewport is expressed in coordinates relative to the control that the event is
        /// raised on.
        /// </remarks>
        public Rect EffectiveViewport { get; private set; }

        /// <summary>
        /// Gets or creates a pooled instance of the event args.
        /// </summary>
        /// <param name="viewport">The effective viewport.</param>
        /// <returns>A reusable event args instance.</returns>
        internal static EffectiveViewportChangedEventArgs GetPooled(Rect viewport)
        {
            var pooled = t_pooled;
            if (pooled != null)
            {
                t_pooled = null;
                pooled.EffectiveViewport = viewport;
                return pooled;
            }
            
            return new EffectiveViewportChangedEventArgs(viewport);
        }

        /// <summary>
        /// Returns the event args to the pool for reuse.
        /// </summary>
        internal void ReturnToPool()
        {
            t_pooled ??= this;
        }
    }
}
