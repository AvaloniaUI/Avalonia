using System;
using Avalonia.Platform;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Reactive;


namespace Avalonia.Media
{
    /// <summary>
    /// Defines a geometric shape.
    /// </summary>
    [TypeConverter(typeof(GeometryTypeConverter))]
    public abstract class Geometry : AvaloniaObject
    {
        /// <summary>
        /// Defines the <see cref="Transform"/> property.
        /// </summary>
        public static readonly StyledProperty<Transform?> TransformProperty =
            AvaloniaProperty.Register<Geometry, Transform?>(nameof(Transform));

        private bool _isDirty = true;
        private readonly bool _canInvaldate = true;
        private IGeometryImpl? _platformImpl;

        static Geometry()
        {
            TransformProperty.Changed.AddClassHandler<Geometry>((x,e) => x.TransformChanged(e));
        }

        internal Geometry()
        {
        }

        private protected Geometry(IGeometryImpl? platformImpl)
        {
            _platformImpl = platformImpl;
           _isDirty = _canInvaldate = false;
        }

        /// <summary>
        /// Raised when the geometry changes.
        /// </summary>
        public event EventHandler? Changed;

        /// <summary>
        /// Gets the geometry's bounding rectangle.
        /// </summary>
        public Rect Bounds => PlatformImpl?.Bounds ?? default;

        /// <summary>
        /// Gets the platform-specific implementation of the geometry.
        /// </summary>
        internal IGeometryImpl? PlatformImpl
        {
            get
            {
                if (_isDirty)
                {
                    var geometry = CreateDefiningGeometry();
                    var transform = Transform;

                    if (geometry != null && transform != null && transform.Value != Matrix.Identity)
                    {
                        geometry = geometry.WithTransform(transform.Value);
                    }

                    _platformImpl = geometry;
                    _isDirty = false;
                }

                return _platformImpl;
            }
        }

        /// <summary>
        /// Gets or sets a transform to apply to the geometry.
        /// </summary>
        public Transform? Transform
        {
            get { return GetValue(TransformProperty); }
            set { SetValue(TransformProperty, value); }
        }

        /// <summary>
        /// Creates a <see cref="Geometry"/> from a string.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <returns>A <see cref="StreamGeometry"/>.</returns>
        public static Geometry Parse(string s) => StreamGeometry.Parse(s);

        /// <summary>
        /// Clones the geometry.
        /// </summary>
        /// <returns>A cloned geometry.</returns>
        public abstract Geometry Clone();

        /// <summary>
        /// Gets the geometry's bounding rectangle with the specified pen.
        /// </summary>
        /// <param name="pen">The stroke thickness.</param>
        /// <returns>The bounding rectangle.</returns>
        public Rect GetRenderBounds(IPen pen) => PlatformImpl?.GetRenderBounds(pen) ?? default;

        /// <summary>
        /// Indicates whether the geometry's fill contains the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool FillContains(Point point)
        {
            return PlatformImpl?.FillContains(point) == true;
        }

        /// <summary>
        /// Indicates whether the geometry's stroke contains the specified point.
        /// </summary>
        /// <param name="pen">The pen to use.</param>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the geometry contains the point; otherwise, <c>false</c>.</returns>
        public bool StrokeContains(IPen pen, Point point)
        {
            return PlatformImpl?.StrokeContains(pen, point) == true;
        }

        /// <summary>
        /// Gets a <see cref="Geometry"/> that is the shape defined by the stroke on the Geometry
        /// produced by the specified Pen.
        /// </summary>
        /// <param name="pen">The pen to use.</param>
        /// <returns>The outlined geometry.</returns>
        public Geometry GetWidenedGeometry(IPen pen)
        {
            return new ImmutableGeometry(PlatformImpl?.GetWidenedGeometry(pen));
        }

        /// <summary>
        /// Marks a property as affecting the geometry's <see cref="PlatformImpl"/>.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a control's static constructor, any change to the
        /// property will cause <see cref="InvalidateGeometry"/> to be called on the element.
        /// </remarks>
        protected static void AffectsGeometry(params AvaloniaProperty[] properties)
        {
            var invalidateObserver = new AnonymousObserver<AvaloniaPropertyChangedEventArgs>(AffectsGeometryInvalidate);
            foreach (var property in properties)
            {
                property.Changed.Subscribe(invalidateObserver);
            }
        }

        /// <summary>
        /// Creates the platform implementation of the geometry, without the transform applied.
        /// </summary>
        /// <returns></returns>
        private protected abstract IGeometryImpl? CreateDefiningGeometry();

        /// <summary>
        /// Invalidates the platform implementation of the geometry.
        /// </summary>
        protected void InvalidateGeometry()
        {
            if (!_canInvaldate)
                return;

            _isDirty = true;
            _platformImpl = null;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void TransformChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldValue = (Transform?)e.OldValue;
            var newValue = (Transform?)e.NewValue;

            if (oldValue != null)
            {
                oldValue.Changed -= TransformChanged;
            }

            if (newValue != null)
            {
                newValue.Changed += TransformChanged;
            }

            TransformChanged(newValue, EventArgs.Empty);
        }

        private void TransformChanged(object? sender, EventArgs e)
        {
            var transform = ((Transform?)sender)?.Value;

            if (_platformImpl is ITransformedGeometryImpl t)
            {
                if (transform == null || transform == Matrix.Identity)
                {
                    _platformImpl = t.SourceGeometry;
                }
                else if (transform != t.Transform)
                {
                    _platformImpl = t.SourceGeometry.WithTransform(transform.Value);
                }
            }
            else if (_platformImpl != null && transform != null && transform != Matrix.Identity)
            {
                _platformImpl = _platformImpl.WithTransform(transform.Value);
            }

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private static void AffectsGeometryInvalidate(AvaloniaPropertyChangedEventArgs e)
        {
            var control = e.Sender as Geometry;
            control?.InvalidateGeometry();
        }

        /// <summary>
        /// Gets the geometry's total length as if all its contours are placed
        /// in a straight line.
        /// </summary>
        public double ContourLength => PlatformImpl?.ContourLength ?? 0;

        /// <summary>
        /// Combines the two geometries using the specified <see cref="GeometryCombineMode"/> and applies the specified transform to the resulting geometry.
        /// </summary>
        /// <param name="geometry1">The first geometry to combine.</param>
        /// <param name="geometry2">The second geometry to combine.</param>
        /// <param name="combineMode">One of the enumeration values that specifies how the geometries are combined.</param>
        /// <param name="transform">A transformation to apply to the combined geometry, or <c>null</c>.</param>
        /// <returns></returns>
        public static Geometry Combine(Geometry geometry1, RectangleGeometry geometry2, GeometryCombineMode combineMode, Transform? transform = null)
        {
            return new CombinedGeometry(combineMode, geometry1, geometry2, transform);
        }

        /// <summary>
        /// Attempts to get the corresponding point at the
        /// specified distance
        /// </summary>
        /// <param name="distance">The contour distance to get from.</param>
        /// <param name="point">The point in the specified distance.</param>
        /// <returns>If there's valid point at the specified distance.</returns>
        public bool TryGetPointAtDistance(double distance, out Point point)
        {
            if (PlatformImpl == null)
            {
                point = default;
                return false;
            }

            return PlatformImpl.TryGetPointAtDistance(distance, out point);
        }

        /// <summary>
        /// Attempts to get the corresponding point and
        /// tangent from the specified distance along the
        /// contour of the geometry.
        /// </summary>
        /// <param name="distance">The contour distance to get from.</param>
        /// <param name="point">The point in the specified distance.</param>
        /// <param name="tangent">The tangent in the specified distance.</param>
        /// <returns>If there's valid point and tangent at the specified distance.</returns>
        public bool TryGetPointAndTangentAtDistance(double distance, out Point point, out Point tangent)
        {
            if (PlatformImpl == null)
            {
                point = tangent = default;
                return false;
            }

            return PlatformImpl.TryGetPointAndTangentAtDistance(distance, out point, out tangent);
        }

        /// <summary>
        /// Attempts to get the corresponding path segment
        /// given by the two distances specified.
        /// Imagine it like snipping a part of the current
        /// geometry.
        /// </summary>
        /// <param name="startDistance">The contour distance to start snipping from.</param>
        /// <param name="stopDistance">The contour distance to stop snipping to.</param>
        /// <param name="startOnBeginFigure">If ture, the resulting snipped path will start with a BeginFigure call.</param>
        /// <param name="segmentGeometry">The resulting snipped path.</param>
        /// <returns>If the snipping operation is successful.</returns>
        public bool TryGetSegment(double startDistance, double stopDistance, bool startOnBeginFigure,
            [NotNullWhen(true)] out Geometry? segmentGeometry)
        {
            segmentGeometry = null;
            if (PlatformImpl == null)
                return false;
            if (!PlatformImpl.TryGetSegment(startDistance, stopDistance, startOnBeginFigure, out var segment))
                return false;
            segmentGeometry = new PlatformGeometry(segment);
            return true;
        }
    }

    public class GeometryTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is null)
            {
                throw GetConvertFromException(value);
            }
            string? source = value as string;

            if (source != null)
            {
                return Geometry.Parse(source);
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
