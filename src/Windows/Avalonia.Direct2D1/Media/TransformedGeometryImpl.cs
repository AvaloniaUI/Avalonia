using Avalonia.Platform;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    internal class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="source">The source geometry.</param>
        /// <param name="geometry">An existing Direct2D <see cref="TransformedGeometry"/>.</param>
        public TransformedGeometryImpl(TransformedGeometry geometry, GeometryImpl source)
            : base(geometry)
        {
            SourceGeometry = source;
        }

        public IGeometryImpl SourceGeometry { get; }

        /// <inheritdoc/>
        public Matrix Transform => ((TransformedGeometry)Geometry).Transform.ToAvalonia();

        protected override Geometry GetSourceGeometry() => ((TransformedGeometry)Geometry).SourceGeometry;
    }
}
