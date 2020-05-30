namespace Avalonia.Media
{
    public interface IExperimentalAcrylicBrush : IBrush
    {
        AcrylicBackgroundSource BackgroundSource { get; }

        Color TintColor { get; }

        double TintOpacity { get; }

        double TintLuminosityOpacity { get; }

        Color FallbackColor { get; }
    }
}
