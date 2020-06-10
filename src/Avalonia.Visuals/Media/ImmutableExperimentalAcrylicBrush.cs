using System;

namespace Avalonia.Media
{
    public readonly struct ImmutableExperimentalAcrylicBrush : IExperimentalAcrylicBrush, IEquatable<ImmutableExperimentalAcrylicBrush>
    {
        public ImmutableExperimentalAcrylicBrush(IExperimentalAcrylicBrush brush)
        {            
            BackgroundSource = brush.BackgroundSource;
            TintColor = brush.TintColor;
            TintOpacity = brush.TintOpacity;
            FallbackColor = brush.FallbackColor;
            Opacity = brush.Opacity;
            LuminosityColor = brush.LuminosityColor;
        }

        public AcrylicBackgroundSource BackgroundSource { get; }

        public Color TintColor { get; }

        public Color LuminosityColor { get; }

        public double TintOpacity { get; }

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
                hash = (hash * 23) + Opacity.GetHashCode();
                hash = (hash * 23) + TintOpacity.GetHashCode();
                hash = (hash * 23) + BackgroundSource.GetHashCode();
                hash = (hash * 23) + FallbackColor.GetHashCode();

                return hash;
            }
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
