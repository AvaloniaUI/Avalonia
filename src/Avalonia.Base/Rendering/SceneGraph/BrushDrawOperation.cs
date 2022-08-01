using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Avalonia.Rendering.SceneGraph
{
    /// <summary>
    /// Base class for draw operations that can use a brush.
    /// </summary>
    internal abstract class BrushDrawOperation : DrawOperation
    {
        public BrushDrawOperation(Rect bounds, Matrix transform, IDisposable? aux)
            : base(bounds, transform)
        {
            Aux = aux;
        }

        /// <summary>
        /// Auxiliary data required to draw the brush
        /// </summary>
        public IDisposable? Aux { get; }

        public override void Dispose()
        {
            Aux?.Dispose();
            base.Dispose();
        }
    }
}
