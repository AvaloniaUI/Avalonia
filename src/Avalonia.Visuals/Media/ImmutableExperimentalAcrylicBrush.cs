using System;

namespace Avalonia.Media
{
    public readonly struct ImmutableExperimentalAcrylicBrush : IExperimentalAcrylicBrush, IEquatable<ImmutableExperimentalAcrylicBrush>
    {
        private readonly Color luminosityColor;

        public ImmutableExperimentalAcrylicBrush(IExperimentalAcrylicBrush brush)
        {
            luminosityColor = brush.GetLuminosityColor();
            BackgroundSource = brush.BackgroundSource;
            TintColor = brush.GetEffectiveTintColor();
            TintOpacity = brush.TintOpacity;
            TintLuminosityOpacity = brush.TintLuminosityOpacity;
            FallbackColor = brush.FallbackColor;
            Opacity = brush.Opacity;
        }

        public AcrylicBackgroundSource BackgroundSource { get; }

        public Color TintColor { get; }

        public double TintOpacity { get; }

        public double? TintLuminosityOpacity { get; }

        public Color FallbackColor { get; }

        public double Opacity { get; }

        public bool Equals(ImmutableExperimentalAcrylicBrush other)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return
                TintColor == other.TintColor &&
                Opacity == other.Opacity &&
                TintOpacity == other.TintOpacity &&
                BackgroundSource == other.BackgroundSource &&
                TintLuminosityOpacity == other.TintLuminosityOpacity &&
                FallbackColor == other.FallbackColor;
        }

        public override bool Equals(object obj)
        {
            return obj is ImmutableExperimentalAcrylicBrush other && Equals(other);
        }

        public Color GetEffectiveTintColor()
        {
            return TintColor;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                
                hash = (hash * 23) + TintColor.GetHashCode();
                hash = (hash * 23) + TintLuminosityOpacity.GetHashCode();
                hash = (hash * 23) + Opacity.GetHashCode();
                hash = (hash * 23) + TintOpacity.GetHashCode();
                hash = (hash * 23) + BackgroundSource.GetHashCode();
                hash = (hash * 23) + FallbackColor.GetHashCode();

                return hash;
            }
        }

        public Color GetLuminosityColor()
        {
            return luminosityColor;
        }

        public static bool operator ==(ImmutableExperimentalAcrylicBrush left, ImmutableExperimentalAcrylicBrush right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImmutableExperimentalAcrylicBrush left, ImmutableExperimentalAcrylicBrush right)
        {
            return !left.Equals(right);
        }
    }
}
