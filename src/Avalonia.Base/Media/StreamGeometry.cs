using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of an arbitrarily complex shape.
    /// </summary>
    public class StreamGeometry : Geometry
    {
        private IGeometryImpl? _impl;

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
        private StreamGeometry(IGeometryImpl? impl)
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
            => new StreamGeometry(_impl) { Transform = Transform };

        /// <summary>
        /// Opens the geometry to start defining it.
        /// </summary>
        /// <returns>
        /// A <see cref="StreamGeometryContext"/> which can be used to define the geometry.
        /// </returns>
        /// <remarks>
        /// The new figures are added to the existing ones. The geometry is updated when the context is disposed.
        /// </remarks>
        public StreamGeometryContext Open()
        {
            return new StreamGeometryContext(CreateBuilder(_impl), this);
        }

        internal void SetImpl(IGeometryImpl impl)
        {
            _impl = impl;
            InvalidateGeometry();
        }

        /// <inheritdoc/>
        private protected override IGeometryImpl? CreateDefiningGeometry()
        {
            if (_impl is null)
            {
                using var builder = CreateBuilder(null);
                _impl = builder.ToGeometry();
            }

            return _impl;
        }

        private static IStreamGeometryBuilder CreateBuilder(IGeometryImpl? source)
        {
            var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
            return factory.CreateStreamGeometryBuilder(source);
        }
    }
}
