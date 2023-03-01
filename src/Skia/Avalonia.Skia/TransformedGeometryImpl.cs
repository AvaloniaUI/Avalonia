using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// A Skia implementation of a <see cref="ITransformedGeometryImpl"/>.
    /// </summary>
    internal class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        /// <summary>
        ///  Initializes a new instance of the <see cref="TransformedGeometryImpl"/> class.
        /// </summary>
        /// <param name="source">Source geometry.</param>
        /// <param name="transform">Transform of new geometry.</param>
        public TransformedGeometryImpl(GeometryImpl source, Matrix transform)
        {
            SourceGeometry = source;
            Transform = transform;
            var matrix = transform.ToSKMatrix();

            var transformedPath = StrokePath =  source.StrokePath.Clone();
            transformedPath?.Transform(matrix);
            
            Bounds = transformedPath?.TightBounds.ToAvaloniaRect() ?? default;
            
            if (ReferenceEquals(source.StrokePath, source.FillPath))
                FillPath = transformedPath;
            else if (source.FillPath != null)
            {
                FillPath = transformedPath = source.FillPath.Clone();
                transformedPath.Transform(matrix);
            }
        }

        /// <inheritdoc />
        public override SKPath? StrokePath { get; }
        
        /// <inheritdoc />
        public override SKPath? FillPath { get; }

        /// <inheritdoc />
        public IGeometryImpl SourceGeometry { get; }

        /// <inheritdoc />
        public Matrix Transform { get; }

        /// <inheritdoc />
        public override Rect Bounds { get; }
    }
}
