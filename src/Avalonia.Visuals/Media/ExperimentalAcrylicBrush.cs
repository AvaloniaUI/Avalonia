using System;

namespace Avalonia.Media
{
    public class ExperimentalAcrylicBrush : Brush, IExperimentalAcrylicBrush
    {
        private Color _effectiveTintColor;
        private Color _effectiveLuminosityColor;

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
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            TintOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicBrush>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            TintLuminosityOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicBrush>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
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

        public static readonly StyledProperty<double?> TintLuminosityOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicBrush, double?>(nameof(TintLuminosityOpacity));

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

        Color IExperimentalAcrylicBrush.LuminosityColor => _effectiveLuminosityColor;

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
                tintColor = new Color((byte)(Math.Round(tintColor.A * (((tintOpacity * tintOpacityModifier) * 0.15) + 0.85))), tintColor.R, tintColor.G, tintColor.B);
            }
            else
            {
                tintColor = new Color((byte)(255 * ((255.0 / tintColor.A) * tintOpacity) * tintOpacityModifier), tintColor.R, tintColor.G, tintColor.B);
            }

            return tintColor;
        }

        private static double AdjustOpacity(double opacity)
        {
            var result = Math.Max((1.0 - Math.Pow((1.0 - opacity), 3.85)), 0.92);
            return result;
        }

        private static double GetTintOpacityModifier(Color tintColor)
        {
            // This method supresses the maximum allowable tint opacity depending on the luminosity and saturation of a color by 
            // compressing the range of allowable values - for example, a user-defined value of 100% will be mapped to 45% for pure 
            // white (100% luminosity), 85% for pure black (0% luminosity), and 90% for pure gray (50% luminosity).  The intensity of 
            // the effect increases linearly as luminosity deviates from 50%.  After this effect is calculated, we cancel it out
            // linearly as saturation increases from zero.

            const double midPoint = 0.5; // Mid point of HsvV range that these calculations are based on. This is here for easy tuning.

            double whiteMaxOpacity = 0.45; // 100% luminosity
            double midPointMaxOpacity = 0.90; // 50% luminosity
            double blackMaxOpacity = 0.85; // 0% luminosity
            
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
                    //maxOpacitySuppression *= Math.Max(1 - (hsv.Saturation * 2), 0.0);
                }

                double opacitySuppression = maxOpacitySuppression * normalizedDeviation;

                opacityModifier = midPointMaxOpacity - opacitySuppression;
            }

            return opacityModifier;
        }

        Color GetEffectiveLuminosityColor()
        {            
            double tintOpacity = TintOpacity;

            // Purposely leaving out tint opacity modifier here because GetLuminosityColor needs the *original* tint opacity set by the user.
            var tintColor = new Color((byte)(Math.Round(TintColor.A * tintOpacity)), TintColor.R, TintColor.G, TintColor.B);

            double? luminosityOpacity = TintLuminosityOpacity;

            return GetLuminosityColor(tintColor, luminosityOpacity);
        }

        public static Color FromHsv(HsvColor color)
        {
            float r = 0;
            float g = 0;
            float b = 0;

            var i = (float)Math.Floor(color.Hue * 6f);
            var f = color.Hue * 6f - i;
            var p = color.Value * (1f - color.Saturation);
            var q = color.Value * (1f - f * color.Saturation);
            var t = color.Value * (1f - (1f - f) * color.Saturation);

            switch (i % 6)
            {
                case 0:
                    r = color.Value;
                    g = t;
                    b = p;
                    break;
                case 1:
                    r = q;
                    g = color.Value;
                    b = p;
                    break;
                case 2:
                    r = p;
                    g = color.Value;
                    b = t;
                    break;
                case 3:
                    r = p;
                    g = q;
                    b = color.Value;
                    break;
                case 4:
                    r = t;
                    g = p;
                    b = color.Value;
                    break;
                case 5:
                    r = color.Value;
                    g = p;
                    b = q;
                    break;
            }

            return new Color(Trim(r), Trim(g), Trim(b), 255);
        }

        private static byte Trim(double value)
        {
            value = Math.Min(Math.Floor(value * 256), 255);

            if (value < 0)
            {
                return 0;
            }
            else if (value > 255)
            {
                return 255;
            }

            return (byte)value;
        }

        double Luminosity (Color color)
        {
            return 0.299 * color.R + 0.587 * color.G + 0.114 * color.B;
        }

        // The tintColor passed into this method should be the original, unmodified color created using user values for TintColor + TintOpacity
        Color GetLuminosityColor(Color tintColor, double? luminosityOpacity)
        {
            var luminosityColor = new Color(255, 127, 127, 127);

            var modifier = GetTintOpacityModifier(luminosityColor);

            // If luminosity opacity is specified, just use the values as is
            if (luminosityOpacity.HasValue)
            {                
                return new Color((byte)(255 * Math.Max(Math.Min(luminosityOpacity.Value * modifier, 1.0), 0.0)), luminosityColor.R, luminosityColor.G, luminosityColor.B);
            }
            else
            {
                // Now figure out luminosity opacity
                // Map original *tint* opacity to this range
                const double minLuminosityOpacity = 0.15;
                const double maxLuminosityOpacity = 1.03;

                double luminosityOpacityRangeMax = maxLuminosityOpacity - minLuminosityOpacity;
                double mappedTintOpacity = ((tintColor.A / 255.0) * luminosityOpacityRangeMax) + minLuminosityOpacity;

                // Finally, combine the luminosity opacity and the HsvV-clamped tint color
                return new Color(Trim(Math.Min(mappedTintOpacity * modifier, 1.0)), luminosityColor.R, luminosityColor.G, luminosityColor.B);                                
            }

        }
    }
}
