using System;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an arbitrarily complex shape.
    /// </summary>
    public class StreamGeometry : Geometry
    {
        IStreamGeometryImpl? _impl;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        public StreamGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamGeometry"/> class.
        /// </summary>
        /// <param name="impl">The platform-specific implementation.</param>
        private StreamGeometry(IStreamGeometryImpl impl)
        {
            _impl = impl;
        }

        /// <summary>
        /// Creates a <see cref="StreamGeometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
        public new static StreamGeometry Parse(string s)
        {
            var streamGeometry = new StreamGeometry();

            using (var context = streamGeometry.Open())
            using (var parser = new PathMarkupParser(context))
            {               
                parser.Parse(s);
            }

            return streamGeometry;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new StreamGeometry(StreamImpl.Clone()) { Transform = Transform };
        }

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        public StreamGeometryContext Open()
        {
            return CreateContext();
        }

        private protected virtual StreamGeometryContext CreateContext()
        {
            // Whatever is drawn here changes the shape, so anything already displaying this geometry
            // needs to redraw. That is done when the context is disposed, because at that point the
            // caller has finished drawing. Doing it when the context is handed out would announce a
            // change that nobody has made yet, and a listener looking at the geometry would see a
            // half drawn shape.
            return new StreamGeometryContext(StreamImpl.Open(), InvalidateGeometry);
        }

        // When a Transform is set, the geometry holds a wrapper around the shape instead of the shape
        // itself, so the wrapper is asked for the shape inside it. This starts from PlatformImpl
        // rather than the field below, because PathGeometry derives from this class and builds its
        // shape from its Figures collection, which reading the field would skip.
        private IStreamGeometryImpl StreamImpl => (IStreamGeometryImpl)(PlatformImpl is ITransformedGeometryImpl transformed
            ? transformed.SourceGeometry
            : PlatformImpl!);

        /// <inheritdoc/>
        private protected override IGeometryImpl? CreateDefiningGeometry()
        {
            if (_impl == null)
            {
                var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
                _impl = factory.CreateStreamGeometry();
            }

            return _impl;
        }
    }
}
