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

        public double TintLuminosityOpacity
        {
            get => GetValue(TintLuminosityOpacityProperty);
            set => SetValue(TintLuminosityOpacityProperty, value);
        }

        public override IBrush ToImmutable()
        {
            return new ImmutableExperimentalAcrylicBrush(this);
        }
    }
}
