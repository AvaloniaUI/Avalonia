using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Platform;

namespace Avalonia.Media
{
    public enum GeometryCombineMode
    {
        /// <summary>
        /// The two regions are combined by taking the union of both. The resulting geometry is
        /// geometry A + geometry B.
        /// </summary>
        Union,

        /// <summary>
        /// The two regions are combined by taking their intersection. The new area consists of the
        /// overlapping region between the two geometries.
        /// </summary>
        Intersect,

        /// <summary>
        /// The two regions are combined by taking the area that exists in the first region but not
        /// the second and the area that exists in the second region but not the first. The new
        /// region consists of (A-B) + (B-A), where A and B are geometries.
        /// </summary>
        Xor,

        /// <summary>
        /// The second region is excluded from the first. Given two geometries, A and B, the area of
        /// geometry B is removed from the area of geometry A, producing a region that is A-B.
        /// </summary>
        Exclude,
    }

    /// <summary>
    /// Represents a 2-D geometric shape defined by the combination of two Geometry objects.
    /// </summary>
    public class CombinedGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Geometry1"/> property.
        /// </summary>
        public static readonly StyledProperty<Geometry?> Geometry1Property =
            AvaloniaProperty.Register<CombinedGeometry, Geometry?>(nameof(Geometry1));

        /// <summary>
        /// Defines the <see cref="Geometry2"/> property.
        /// </summary>
        public static readonly StyledProperty<Geometry?> Geometry2Property =
            AvaloniaProperty.Register<CombinedGeometry, Geometry?>(nameof(Geometry2));
        /// <summary>
        /// Defines the <see cref="GeometryCombineMode"/> property.
        /// </summary>
        public static readonly StyledProperty<GeometryCombineMode> GeometryCombineModeProperty =
            AvaloniaProperty.Register<CombinedGeometry, GeometryCombineMode>(nameof(GeometryCombineMode));

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedGeometry"/> class.
        /// </summary>
        public CombinedGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedGeometry"/> class with the
        /// specified <see cref="Geometry"/> objects.
        /// </summary>
        /// <param name="geometry1">The first geometry to combine.</param>
        /// <param name="geometry2">The second geometry to combine.</param>
        public CombinedGeometry(Geometry geometry1, Geometry geometry2)
        {
            Geometry1 = geometry1;
            Geometry2 = geometry2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedGeometry"/> class with the
        /// specified <see cref="Geometry"/> objects and <see cref="GeometryCombineMode"/>.
        /// </summary>
        /// <param name="combineMode">The method by which geometry1 and geometry2 are combined.</param>
        /// <param name="geometry1">The first geometry to combine.</param>
        /// <param name="geometry2">The second geometry to combine.</param>
        public CombinedGeometry(GeometryCombineMode combineMode, Geometry? geometry1, Geometry? geometry2)
        {
            Geometry1 = geometry1;
            Geometry2 = geometry2;
            GeometryCombineMode = combineMode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombinedGeometry"/> class with the
        /// specified <see cref="Geometry"/> objects, <see cref="GeometryCombineMode"/> and
        /// <see cref="Transform"/>.
        /// </summary>
        /// <param name="combineMode">The method by which geometry1 and geometry2 are combined.</param>
        /// <param name="geometry1">The first geometry to combine.</param>
        /// <param name="geometry2">The second geometry to combine.</param>
        /// <param name="transform">The transform applied to the geometry.</param>
        public CombinedGeometry(
            GeometryCombineMode combineMode,
            Geometry? geometry1,
            Geometry? geometry2,
            Transform? transform)
        {
            Geometry1 = geometry1;
            Geometry2 = geometry2;
            GeometryCombineMode = combineMode;
            Transform = transform;
        }

        /// <summary>
        /// Gets or sets the first <see cref="Geometry"/> object of this
        /// <see cref="CombinedGeometry"/> object.
        /// </summary>
        public Geometry? Geometry1
        {
            get => GetValue(Geometry1Property);
            set => SetValue(Geometry1Property, value);
        }

        /// <summary>
        /// Gets or sets the second <see cref="Geometry"/> object of this
        /// <see cref="CombinedGeometry"/> object.
        /// </summary>
        public Geometry? Geometry2
        {
            get => GetValue(Geometry2Property);
            set => SetValue(Geometry2Property, value);
        }

        /// <summary>
        /// Gets or sets the method by which the two geometries (specified by the
        /// <see cref="Geometry1"/> and <see cref="Geometry2"/> properties) are combined. The
        /// default value is <see cref="GeometryCombineMode.Union"/>.
        /// </summary>
        public GeometryCombineMode GeometryCombineMode
        {
            get => GetValue(GeometryCombineModeProperty);
            set => SetValue(GeometryCombineModeProperty, value);
        }

        public override Geometry Clone()
        {
            return new CombinedGeometry(GeometryCombineMode, Geometry1, Geometry2, Transform);
        }

        private protected sealed override IGeometryImpl? CreateDefiningGeometry()
        {
            var g1 = Geometry1;
            var g2 = Geometry2;

            if (g1?.PlatformImpl != null && g2?.PlatformImpl != null)
            {
                var factory = AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
                return factory.CreateCombinedGeometry(GeometryCombineMode, g1.PlatformImpl, g2.PlatformImpl);
            }

            if (GeometryCombineMode == GeometryCombineMode.Intersect)
                return null;
            return g1?.PlatformImpl ?? g2?.PlatformImpl;
        }
    }
}
