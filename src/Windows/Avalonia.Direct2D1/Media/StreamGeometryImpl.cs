using Avalonia.Platform;
using SharpDX.Direct2D1;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A Direct2D implementation of a <see cref="Avalonia.Media.StreamGeometry"/>.
    /// </summary>
    internal class StreamGeometryImpl : GeometryImpl, IStreamGeometryImpl
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
        /// <param name="geometry">An existing Direct2D <see cref="PathGeometry"/>.</param>
        public StreamGeometryImpl(PathGeometry geometry)
            : base(geometry)
        {
        }

        /// <inheritdoc/>
        public IStreamGeometryImpl Clone()
        {
            var result = new PathGeometry(Direct2D1Platform.Direct2D1Factory);
            using (var sink = result.Open())
            {
                ((PathGeometry)Geometry).Stream(sink);
                sink.Close();
            }

            return new StreamGeometryImpl(result);
        }

        /// <inheritdoc/>
        public IStreamGeometryContextImpl Open()
        {
            return new StreamGeometryContextImpl(((PathGeometry)Geometry).Open());
        }

        private static Geometry CreateGeometry()
        {
            return new PathGeometry(Direct2D1Platform.Direct2D1Factory);
        }
    }
}
