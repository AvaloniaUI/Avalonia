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
            var isTransformed = PlatformImpl is ITransformedGeometryImpl;
            var context = new StreamGeometryContext(StreamImpl.Open());

            if (isTransformed)
            {
                // The wrapper holds a transformed copy, so it has to be rebuilt after writing.
                InvalidateGeometry();
            }

            return context;
        }

        // Derived classes build their own defining geometry, so start from PlatformImpl.
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
