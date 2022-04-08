using Avalonia.Platform;
using Vortice.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.StreamGeometry"/>.
    /// </summary>
    public class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        public StreamGeometryImpl()
            : base(CreateGeometry())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometryImpl"/> class.
        /// </summary>
        /// <param name="geometry">An existing Direct2D <see cref="ID2D1PathGeometry"/>.</param>
        public StreamGeometryImpl(ID2D1PathGeometry geometry)
            : base(geometry)
        {
        }

        /// <inheritdoc/>
        public IStreamGeometryImpl Clone()
        {
            var result = Direct2D1Platform.Direct2D1Factory.CreatePathGeometry();
            using (var sink = result.Open())
            {
                ((ID2D1PathGeometry)Geometry).Stream(sink);
                sink.Close();
            }

            return new StreamGeometryImpl(result);
        }

        /// <inheritdoc/>
        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(((ID2D1PathGeometry)Geometry).Open());
        }

        private static ID2D1Geometry CreateGeometry()
        {
            return Direct2D1Platform.Direct2D1Factory.CreatePathGeometry();
        }
    }
}
