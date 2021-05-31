using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    public class TransformedGeometryImpl : GeometryImpl, ITransformedGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="source">The source geometry.</param>
        /// <param name="geometry">An existing Direct2D <see cref="ID2D1TransformedGeometry"/>.</param>
        public TransformedGeometryImpl(ID2D1TransformedGeometry geometry, GeometryImpl source)
            : base(geometry)
        {
            SourceGeometry = source;
        }

        public IGeometryImpl SourceGeometry { get; }

        /// <inheritdoc/>
        public Matrix Transform => ((ID2D1TransformedGeometry)Geometry).Transform.ToAvalonia();

        protected override ID2D1Geometry GetSourceGeometry() => ((ID2D1TransformedGeometry)Geometry).SourceGeometry;
    }
}
