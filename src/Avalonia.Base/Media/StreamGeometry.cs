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

            var builder = CreateBuilder(null);
            using var parser = new PathMarkupParser(builder);

            parser.Parse(s);
            streamGeometry._impl = builder.ToGeometry();

            return streamGeometry;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
            => new StreamGeometry(CreateDefiningGeometry()) { Transform = Transform };

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
            => new(OpenCore());

        private protected virtual IGeometryContext OpenCore()
            => new BuilderWrapper(CreateBuilder(_impl), this);

        private void SetImpl(IGeometryImpl impl)
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

        /// <summary>
        /// Wraps a <see cref="IStreamGeometryBuilder"/> and sets the <see cref="StreamGeometry"/> geometry when disposed.
        /// </summary>
        private sealed class BuilderWrapper(IStreamGeometryBuilder builder, StreamGeometry owner) : IGeometryContext
        {
            public void ArcTo(Point point, Size size, double rotationAngle, bool isLargeArc, SweepDirection sweepDirection, bool isStroked = true)
                => builder.ArcTo(point, size, rotationAngle, isLargeArc, sweepDirection, isStroked);

            public void BeginFigure(Point startPoint, bool isFilled = true)
                => builder.BeginFigure(startPoint, isFilled);

            public void CubicBezierTo(Point controlPoint1, Point controlPoint2, Point endPoint, bool isStroked = true)
                => builder.CubicBezierTo(controlPoint1, controlPoint2, endPoint, isStroked);

            public void QuadraticBezierTo(Point controlPoint, Point endPoint, bool isStroked = true)
                => builder.QuadraticBezierTo(controlPoint, endPoint, isStroked);

            public void LineTo(Point point, bool isStroked = true)
                => builder.LineTo(point, isStroked);

            public void EndFigure(bool isClosed)
                => builder.EndFigure(isClosed);

            public void SetFillRule(FillRule fillRule)
                => builder.SetFillRule(fillRule);

            public void Dispose()
            {
                owner.SetImpl(builder.ToGeometry());
                builder.Dispose();
            }
        }
    }
}
