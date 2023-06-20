using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [PrivateApi]
    public interface IDrawingContextWithAcrylicLikeSupport
    {
        void DrawRectangle(IExperimentalAcrylicMaterial material, RoundedRect rect);
    }
}
