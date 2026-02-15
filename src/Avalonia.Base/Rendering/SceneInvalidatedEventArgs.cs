using System;
using Avalonia.Metadata;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Provides data for the <see cref="IRenderer.SceneInvalidated"/> event.
    /// </summary>
    [PrivateApi]
    public class SceneInvalidatedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SceneInvalidatedEventArgs"/> class.
        /// </summary>
        /// <param name="dirtyRect">The updated area.</param>
        public SceneInvalidatedEventArgs(Rect dirtyRect)
        {
            DirtyRect = dirtyRect;
        }

        /// <summary>
        /// Gets the invalidated area.
        /// </summary>
        public Rect DirtyRect { get; }

    }
}
