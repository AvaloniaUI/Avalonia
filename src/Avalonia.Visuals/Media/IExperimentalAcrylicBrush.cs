namespace Avalonia.Media
{
    public interface IExperimentalAcrylicBrush : IBrush
    {
        AcrylicBackgroundSource BackgroundSource { get; set; }

        Color TintColor { get; set; }

        double TintOpacity { get; set; }

        double TintLuminosityOpacity { get; set; }

        Color FallbackColor { get; set; }
    }
}
