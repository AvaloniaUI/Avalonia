using System.Drawing;

namespace Avalonia.Media
{
    public interface IExperimentalAcrylicBrush : IBrush
    {
        AcrylicBackgroundSource BackgroundSource { get; }

        Color TintColor { get; }
        
        Color LuminosityColor { get; }

        double TintOpacity { get; }

        Color FallbackColor { get; }
    }
}
