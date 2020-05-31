using System;

namespace Avalonia.Media
{
    public class ExperimentalAcrylicBrush : Brush, IExperimentalAcrylicBrush
    {
        private Color _effectiveTintColor;

        static ExperimentalAcrylicBrush()
        {
            AffectsRender<ExperimentalAcrylicBrush>(
                TintColorProperty,
                BackgroundSourceProperty,
                TintOpacityProperty,
                TintLuminosityOpacityProperty);

            TintColorProperty.Changed.AddClassHandler<ExperimentalAcrylicBrush>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
            });

            TintOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicBrush>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
            });
        }
        
        /// <summary>
        /// Defines the <see cref="TintColor"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> TintColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, Color>(nameof(TintColor));

        public static readonly StyledProperty<AcrylicBackgroundSource> BackgroundSourceProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, AcrylicBackgroundSource>(nameof(BackgroundSource));

        public static readonly StyledProperty<double> TintOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, double>(nameof(TintOpacity));

        public static readonly StyledProperty<double> TintLuminosityOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, double>(nameof(TintLuminosityOpacity));

        public static readonly StyledProperty<Color> FallbackColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, Color>(nameof(FallbackColor));

        public AcrylicBackgroundSource BackgroundSource
        {
            get => GetValue(BackgroundSourceProperty);
            set => SetValue(BackgroundSourceProperty, value);
        }

        public Color TintColor
        {
            get => GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        Color IExperimentalAcrylicBrush.TintColor => _effectiveTintColor;

        public double TintOpacity
        {
            get => GetValue(TintOpacityProperty);
            set => SetValue(TintOpacityProperty, value);
        }

        public Color FallbackColor
        {
            get => GetValue(FallbackColorProperty);
            set => SetValue(FallbackColorProperty, value);
        }

        public double? TintLuminosityOpacity
        {
            get => GetValue(TintLuminosityOpacityProperty);
            set => SetValue(TintLuminosityOpacityProperty, value);
        }

        public override IBrush ToImmutable()
        {
            return new ImmutableExperimentalAcrylicBrush(this);
        }

        public struct HsvColor
        {
            public float Hue { get; set; }
            public float Saturation { get; set; }
            public float Value { get; set; }
        }

        public static HsvColor RgbToHsv(Color color)
        {
            var r = color.R /255.0f;
            var g = color.G / 255.0f;
            var b = color.B / 255.0f;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));

            float h, s, v;
            h = s = v = max;

            //v = (0.299f * r + 0.587f * g + 0.114f * b);

            var d = max - min;
            s = max == 0 ? 0 : d / max;

            if (max == min)
            {
                h = 0; // achromatic
            }
            else
            {
                if (max == r)
                {
                    h = (g - b) / d + (g < b ? 6 : 0);
                }
                else if (max == g)
                {
                    h = (b - r) / d + 2;
                }
                else if (max == b)
                {
                    h = (r - g) / d + 4;
                }

                h /= 6;
            }

            return new HsvColor { Hue = h, Saturation = s, Value = v };
        }

        private static Color GetEffectiveTintColor(Color tintColor, double tintOpacity)
        {
            // Update tintColor's alpha with the combined opacity value
            double tintOpacityModifier = GetTintOpacityModifier(tintColor);

            if (false) // non-acrylic blue // TODO detect blur level.
            {
                tintColor = new Color((byte)(Math.Round(tintColor.A * (((tintOpacity * tintOpacityModifier) * 0.25) + 0.75))), tintColor.R, tintColor.G, tintColor.B);
            }
            else
            {
                tintColor = new Color((byte)(Math.Round(tintColor.A * tintOpacity * tintOpacityModifier)), tintColor.R, tintColor.G, tintColor.B);
            }

            return tintColor;
        }

        private static double GetTintOpacityModifier(Color tintColor)
        {
            // This method supresses the maximum allowable tint opacity depending on the luminosity and saturation of a color by 
            // compressing the range of allowable values - for example, a user-defined value of 100% will be mapped to 45% for pure 
            // white (100% luminosity), 85% for pure black (0% luminosity), and 90% for pure gray (50% luminosity).  The intensity of 
            // the effect increases linearly as luminosity deviates from 50%.  After this effect is calculated, we cancel it out
            // linearly as saturation increases from zero.

            const double midPoint = 0.5; // Mid point of HsvV range that these calculations are based on. This is here for easy tuning.

            const double whiteMaxOpacity = 0.40; // 100% luminosity
            const double midPointMaxOpacity = 0.50; // 50% luminosity
            const double blackMaxOpacity = 0.60; // 0% luminosity
            
            var hsv = RgbToHsv(tintColor);

            double opacityModifier = midPointMaxOpacity;

            if (hsv.Value != midPoint)
            {
                // Determine maximum suppression amount
                double lowestMaxOpacity = midPointMaxOpacity;
                double maxDeviation = midPoint;

                if (hsv.Value > midPoint)
                {
                    lowestMaxOpacity = whiteMaxOpacity; // At white (100% hsvV)
                    maxDeviation = 1 - maxDeviation;
                }
                else if (hsv.Value < midPoint)
                {
                    lowestMaxOpacity = blackMaxOpacity; // At black (0% hsvV)
                }

                double maxOpacitySuppression = midPointMaxOpacity - lowestMaxOpacity;

                // Determine normalized deviation from the midpoint
                double deviation = Math.Abs(hsv.Value - midPoint);
                double normalizedDeviation = deviation / maxDeviation;

                // If we have saturation, reduce opacity suppression to allow that color to come through more
                if (hsv.Saturation > 0)
                {
                    // Dampen opacity suppression based on how much saturation there is
                    maxOpacitySuppression *= Math.Max(1 - (hsv.Saturation * 2), 0.0);
                }

                double opacitySuppression = maxOpacitySuppression * normalizedDeviation;

                opacityModifier = midPointMaxOpacity - opacitySuppression;
            }

            return opacityModifier;
        }
    }
}
