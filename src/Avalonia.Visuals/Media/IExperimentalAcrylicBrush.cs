using System.Drawing;

namespace Avalonia.Media
{
    public interface IExperimentalAcrylicBrush : IBrush
    {
        AcrylicBackgroundSource BackgroundSource { get; }

        Color TintColor { get; }        

        double TintOpacity { get; }

        Color FallbackColor { get; }
    }
}
