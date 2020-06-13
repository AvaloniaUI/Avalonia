using System;

namespace Avalonia.Media
{
    public class ExperimentalAcrylicMaterial : AvaloniaObject, IMutableExperimentalAcrylicMaterial
    {
        private Color _effectiveTintColor;
        private Color _effectiveLuminosityColor;

        static ExperimentalAcrylicMaterial()
        {
            AffectsRender<ExperimentalAcrylicMaterial>(
                TintColorProperty,
                BackgroundSourceProperty,
                TintOpacityProperty,
                MaterialOpacityProperty,
                PlatformTransparencyCompensationLevelProperty);

            TintColorProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            TintOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            MaterialOpacityProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });

            PlatformTransparencyCompensationLevelProperty.Changed.AddClassHandler<ExperimentalAcrylicMaterial>((b, e) =>
            {
                b._effectiveTintColor = GetEffectiveTintColor(b.TintColor, b.TintOpacity);
                b._effectiveLuminosityColor = b.GetEffectiveLuminosityColor();
            });
        }

        /// <summary>
        /// Defines the <see cref="TintColor"/> property.
        /// </summary>
        public static readonly StyledProperty<Color> TintColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, Color>(nameof(TintColor));

        public static readonly StyledProperty<AcrylicBackgroundSource> BackgroundSourceProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, AcrylicBackgroundSource>(nameof(BackgroundSource));

        public static readonly StyledProperty<double> TintOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(TintOpacity), 0.8);

        public static readonly StyledProperty<double> MaterialOpacityProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(MaterialOpacity), 0.5);

        public static readonly StyledProperty<double> PlatformTransparencyCompensationLevelProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, double>(nameof(PlatformTransparencyCompensationLevel), 0.0);

        public static readonly StyledProperty<Color> FallbackColorProperty =
            AvaloniaProperty.Register<ExperimentalAcrylicMaterial, Color>(nameof(FallbackColor));

        /// <inheritdoc/>
        public event EventHandler Invalidated;

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

        public double MaterialOpacity
        {
            get => GetValue(MaterialOpacityProperty);
            set => SetValue(MaterialOpacityProperty, value);
        }

        public double PlatformTransparencyCompensationLevel
        {
            get => GetValue(PlatformTransparencyCompensationLevelProperty);
            set => SetValue(PlatformTransparencyCompensationLevelProperty, value);
        }

        Color IExperimentalAcrylicMaterial.MaterialColor => _effectiveLuminosityColor;

        Color IExperimentalAcrylicMaterial.TintColor => _effectiveTintColor;

        public struct HsvColor
        {
            public float Hue { get; set; }
            public float Saturation { get; set; }
            public float Value { get; set; }
        }

        public static HsvColor RgbToHsv(Color color)
        {
            var r = color.R / 255.0f;
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

        private static double GetTintOpacityModifier(Color tintColor)
        {
            // This method supresses the maximum allowable tint opacity depending on the luminosity and saturation of a color by 
            // compressing the range of allowable values - for example, a user-defined value of 100% will be mapped to 45% for pure 
            // white (100% luminosity), 85% for pure black (0% luminosity), and 90% for pure gray (50% luminosity).  The intensity of 
            // the effect increases linearly as luminosity deviates from 50%.  After this effect is calculated, we cancel it out
            // linearly as saturation increases from zero.

            const double midPoint = 0.5; // Mid point of HsvV range that these calculations are based on. This is here for easy tuning.

            const double whiteMaxOpacity = 0.2; // 100% luminosity
            const double midPointMaxOpacity = 0.45; // 50% luminosity
            const double blackMaxOpacity = 0.42; // 0% luminosity

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

        Color GetEffectiveLuminosityColor()
        {
            double tintOpacity = TintOpacity;

            // Purposely leaving out tint opacity modifier here because GetLuminosityColor needs the *original* tint opacity set by the user.
            var tintColor = new Color((byte)(Math.Round(TintColor.A * tintOpacity)), TintColor.R, TintColor.G, TintColor.B);

            double? luminosityOpacity = MaterialOpacity;

            return GetLuminosityColor(luminosityOpacity);
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

        private static float RGBMax(Color color)
        {
            if (color.R > color.G)
                return (color.R > color.B) ? color.R : color.B;
            else
                return (color.G > color.B) ? color.G : color.B;
        }

        private static float RGBMin(Color color)
        {
            if (color.R < color.G)
                return (color.R < color.B) ? color.R : color.B;
            else
                return (color.G < color.B) ? color.G : color.B;
        }

        static byte Blend(byte leftColor, byte leftAlpha, byte rightColor, byte rightAlpha)
        {
            var ca = leftColor / 255d;
            var aa = leftAlpha / 255d;
            var cb = rightColor / 255d;
            var ab = rightAlpha / 255d;
            var r = (ca * aa + cb * ab * (1 - aa)) / (aa + ab * (1 - aa));
            return (byte)(r * 255);
        }

        static Color Blend(Color left, Color right)
        {
            var aa = left.A / 255d;
            var ab = right.A / 255d;
            return new Color(
                (byte)((aa + ab * (1 - aa)) * 255),
                Blend(left.R, left.A, right.R, right.A),
                Blend(left.G, left.A, right.G, right.A),
                Blend(left.B, left.A, right.B, right.A)
            );
        }

        // The tintColor passed into this method should be the original, unmodified color created using user values for TintColor + TintOpacity
        Color GetLuminosityColor(double? luminosityOpacity)
        {
            // Calculate the HSL lightness value of the color.
            var max = (float)RGBMax(TintColor) / 255.0f;
            var min = (float)RGBMin(TintColor) / 255.0f;

            var lightness = (max + min) / 2.0;

            lightness = 1 - ((1 - lightness) * luminosityOpacity.Value);

            lightness = 0.13 + (lightness * 0.74);

            var luminosityColor = new Color(255, Trim(lightness), Trim(lightness), Trim(lightness));

            luminosityColor = Blend(luminosityColor, new Color(255, TintColor.R, TintColor.G, TintColor.B));

            var compensationMultiplier = 1 - PlatformTransparencyCompensationLevel;
            return new Color((byte)(255 * Math.Max(Math.Min(PlatformTransparencyCompensationLevel + ( luminosityOpacity.Value * compensationMultiplier), 1.0), 0.0)), luminosityColor.R, luminosityColor.G, luminosityColor.B);            
        }

        /// <summary>
        /// Marks a property as affecting the brush's visual representation.
        /// </summary>
        /// <param name="properties">The properties.</param>
        /// <remarks>
        /// After a call to this method in a brush's static constructor, any change to the
        /// property will cause the <see cref="Invalidated"/> event to be raised on the brush.
        /// </remarks>
        protected static void AffectsRender<T>(params AvaloniaProperty[] properties)
            where T : ExperimentalAcrylicMaterial
        {
            static void Invalidate(AvaloniaPropertyChangedEventArgs e)
            {
                (e.Sender as T)?.RaiseInvalidated(EventArgs.Empty);
            }

            foreach (var property in properties)
            {
                property.Changed.Subscribe(e => Invalidate(e));
            }
        }

        /// <summary>
        /// Raises the <see cref="Invalidated"/> event.
        /// </summary>
        /// <param name="e">The event args.</param>
        protected void RaiseInvalidated(EventArgs e) => Invalidated?.Invoke(this, e);

        public IExperimentalAcrylicMaterial ToImmutable()
        {
            return new ImmutableExperimentalAcrylicMaterial(this);
        }
    }
}
