using Avalonia.Utilities;

namespace Avalonia.Media.Immutable
{
    /// <summary>
    /// Describes the location and color of a transition point in a gradient.
    /// </summary>
    public class ImmutableGradientStop : IGradientStop
    {
        public ImmutableGradientStop(double offset, Color color)
        {
            if (MathUtilities.IsZero(offset))
            {
                offset = 0;
            }

            Offset = (offset < 0) ? 0 : (offset > 1) ? 1 : offset;
            Color = color;
        }

        /// <inheritdoc/>
        public double Offset { get; }

        /// <inheritdoc/>
        public Color Color { get; }
    }
}
