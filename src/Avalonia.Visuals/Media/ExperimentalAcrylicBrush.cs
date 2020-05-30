namespace Avalonia.Media
{
    public class ExperimentalAcrylicBrush : IExperimentalAcrylicBrush
    {
        public AcrylicBackgroundSource BackgroundSource { get; set; }

        public Color TintColor { get; set; }

        public double TintOpacity { get; set; }

        public Color FallbackColor { get; set; }

        public double TintLuminosityOpacity { get; set; }

        public double Opacity { get; set; }
    }
}
