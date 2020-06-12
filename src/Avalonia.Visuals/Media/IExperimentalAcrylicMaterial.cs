namespace Avalonia.Media
{
    public interface IExperimentalAcrylicMaterial
    {
        AcrylicBackgroundSource BackgroundSource { get; }

        Color TintColor { get; }

        Color LuminosityColor { get; }

        double TintOpacity { get; }

        Color FallbackColor { get; }
    }
}
