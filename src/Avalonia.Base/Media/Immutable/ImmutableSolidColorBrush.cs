using System;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Fills an area with a solid color.
    /// </summary>
    public class ImmutableSolidColorBrush : IImmutableSolidColorBrush, IEquatable<ImmutableSolidColorBrush>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="transform">The transform of the brush.</param>
        // TODO13: remove, folding relativeTransform into the overload below as an optional parameter.
        public ImmutableSolidColorBrush(Color color, double opacity = 1, ImmutableTransform? transform = null)
            : this(color, opacity, transform, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        /// <param name="opacity">The opacity of the brush.</param>
        /// <param name="transform">The transform of the brush.</param>
        /// <param name="relativeTransform">The transform of the brush in the relative coordinate
        /// space of the area being painted, applied before <paramref name="transform"/>.</param>
        public ImmutableSolidColorBrush(
            Color color,
            double opacity,
            ImmutableTransform? transform,
            ImmutableTransform? relativeTransform)
        {
            Color = color;
            Opacity = opacity;
            Transform = transform;
            RelativeTransform = relativeTransform;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="color">The color to use.</param>
        public ImmutableSolidColorBrush(uint color)
            : this(Color.FromUInt32(color))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImmutableSolidColorBrush"/> class.
        /// </summary>
        /// <param name="source">The brush from which this brush's properties should be copied.</param>
        public ImmutableSolidColorBrush(ISolidColorBrush source)
            : this(source.Color, source.Opacity, source.Transform?.ToImmutable(),
                  source.RelativeTransform?.ToImmutable())
        {
        }

        /// <summary>
        /// Gets the color of the brush.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the opacity of the brush.
        /// </summary>
        public double Opacity { get; }

        /// <summary>
        /// Gets the transform of the brush.
        /// </summary>
        public ITransform? Transform { get; }

        /// <summary>
        /// Gets the transform origin of the brush
        /// </summary>
        public RelativePoint TransformOrigin { get; }

        /// <inheritdoc/>
        public ITransform? RelativeTransform { get; }

        public bool Equals(ImmutableSolidColorBrush? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Color.Equals(other.Color) && Opacity.Equals(other.Opacity)
                   && Equals(Transform, other.Transform) && Equals(RelativeTransform, other.RelativeTransform);
        }

        public override bool Equals(object? obj)
        {
            return obj is ImmutableSolidColorBrush other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Color.GetHashCode() * 397) ^ Opacity.GetHashCode() ^ (Transform is null ? 0 : Transform.GetHashCode())
                       ^ (RelativeTransform is null ? 0 : RelativeTransform.GetHashCode());
            }
        }

        public static bool operator ==(ImmutableSolidColorBrush left, ImmutableSolidColorBrush right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ImmutableSolidColorBrush left, ImmutableSolidColorBrush right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Returns a string representation of the brush.
        /// </summary>
        /// <returns>A string representation of the brush.</returns>
        public override string ToString()
        {
            return Color.ToString();
        }
    }
}
