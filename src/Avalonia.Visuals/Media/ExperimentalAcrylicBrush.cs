using System;

namespace Avalonia.Media
{
    public class ExperimentalAcrylicBrush : Brush, IExperimentalAcrylicBrush
    {
        static ExperimentalAcrylicBrush()
        {
            AffectsRender<ExperimentalAcrylicBrush>(
                TintColorProperty,
                BackgroundSourceProperty,
                TintOpacityProperty,
                TintLuminosityOpacityProperty);
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

        public static Color FromHsv(HsvColor color)
        {
            var i = (float)Math.Floor(color.Hue * 6f);
            var f = color.Hue * 6f - i;
            var p = color.Value * (1f - color.Saturation);
            var q = color.Value * (1f - f * color.Saturation);
            var t = color.Value * (1f - (1f - f) * color.Saturation);

            switch (i % 6)
            {
                case 0:
                    return new Color(255, (byte)(255.0 * color.Value), (byte)(255.0 * t), (byte)(255.0 * p));
                case 1:
                    return new Color(255, (byte)(255.0 * q), (byte)(255.0 * color.Value), (byte)(255.0 * p));
                case 2:
                    return new Color(255, (byte)(255.0 * p), (byte)(255.0 * color.Value), (byte)(255.0 * t));
                case 3:
                    return new Color(255, (byte)(255.0 * p), (byte)(255.0 * q), (byte)(255.0 * color.Value));
                case 4:
                    return new Color(255, (byte)(255.0 * t), (byte)(255.0 * p), (byte)(255.0 * color.Value));
                default:
                case 5:
                    return new Color(255, (byte)(255.0 * color.Value), (byte)(255.0 * p), (byte)(255.0 * q));
            }
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

        public Color GetLuminosityColor ()
        {
            return GetLuminosityColor(TintColor, TintLuminosityOpacity);
        }

        Color GetLuminosityColor(Color tintColor, double? luminosityOpacity)
        {
            var rgbTintColor = tintColor;

            // If luminosity opacity is specified, just use the values as is
            if (luminosityOpacity.HasValue)
            {
                return new Color((byte)(255.0 * Math.Min(1.0, Math.Max(0.0, luminosityOpacity.Value))), tintColor.R, tintColor.G, tintColor.B);
            }
            else
            {
                // To create the Luminosity blend input color without luminosity opacity,
                // we're taking the TintColor input, converting to HSV, and clamping the V between these values
                const double minHsvV = 0.125;
                const double maxHsvV = 0.965;

                var hsvTintColor = RgbToHsv(rgbTintColor);

                var clampedHsvV = Math.Max(Math.Min(hsvTintColor.Value, minHsvV), maxHsvV);                
                var hsvLuminosityColor = hsvTintColor;
                hsvLuminosityColor.Value = (float)clampedHsvV;

                var rgbLuminosityColor = FromHsv(hsvLuminosityColor);

                // Now figure out luminosity opacity
                // Map original *tint* opacity to this range
                const double minLuminosityOpacity = 0.15;
                const double maxLuminosityOpacity = 1.03;

                double luminosityOpacityRangeMax = maxLuminosityOpacity - minLuminosityOpacity;
                double mappedTintOpacity = ((tintColor.A / 255.0) * luminosityOpacityRangeMax) + minLuminosityOpacity;

                // Finally, combine the luminosity opacity and the HsvV-clamped tint color                
                return  new Color((byte)(255.0 * Math.Min(mappedTintOpacity, 1.0)), rgbLuminosityColor.R, rgbLuminosityColor.G, rgbLuminosityColor.B);
            }

        }


        public Color GetEffectiveTintColor()
        {
            var tintColor = TintColor;
            double tintOpacity = TintOpacity;

            // Update tintColor's alpha with the combined opacity value
            // If LuminosityOpacity was specified, we don't intervene into users parameters
            if (false)//TintLuminosityOpacity() != nullptr)
            {
                //tintColor.A = static_cast<uint8_t>(round(tintColor.A * tintOpacity));
            }
            else
            {
                double tintOpacityModifier = GetTintOpacityModifier(tintColor);

                tintColor = new Color((byte)(Math.Round(tintColor.A * tintOpacity * tintOpacityModifier)), tintColor.R, tintColor.G, tintColor.B);
            }

            return tintColor;
        }

        double GetTintOpacityModifier(Color tintColor)
        {
            // This method supresses the maximum allowable tint opacity depending on the luminosity and saturation of a color by 
            // compressing the range of allowable values - for example, a user-defined value of 100% will be mapped to 45% for pure 
            // white (100% luminosity), 85% for pure black (0% luminosity), and 90% for pure gray (50% luminosity).  The intensity of 
            // the effect increases linearly as luminosity deviates from 50%.  After this effect is calculated, we cancel it out
            // linearly as saturation increases from zero.

            const double midPoint = 0.50; // Mid point of HsvV range that these calculations are based on. This is here for easy tuning.

            const double whiteMaxOpacity = 0.45; // 100% luminosity
            const double midPointMaxOpacity = 0.90; // 50% luminosity
            const double blackMaxOpacity = 0.85; // 0% luminosity
            
            var hsv = RgbToHsv(tintColor);

            if(tintColor == Colors.Red)
            {

            }

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
