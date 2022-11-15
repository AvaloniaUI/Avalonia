﻿using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Paints an area with an <see cref="IVisual"/>.
    /// </summary>
    [NotClientImplementable]
    public interface IVisualBrush : ITileBrush
    {
        /// <summary>
        /// Gets the visual to draw.
        /// </summary>
        IVisual Visual { get; }
    }
}
