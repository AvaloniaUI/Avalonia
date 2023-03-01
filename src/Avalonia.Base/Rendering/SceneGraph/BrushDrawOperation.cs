using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperationWithTransform
    {
        public IImmutableBrush? Brush { get; }

        public BrushDrawOperation(Rect bounds, Matrix transform, IImmutableBrush? brush)
            : base(bounds, transform)
        {
            Brush = brush;
        }

        public override void Dispose()
        {
            (Brush as ISceneBrushContent)?.Dispose();
            base.Dispose();
        }
    }
}
