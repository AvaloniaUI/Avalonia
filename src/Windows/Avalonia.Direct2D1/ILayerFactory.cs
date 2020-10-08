using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    public interface ILayerFactory
    {
        IDrawingContextLayerImpl CreateLayer(Size size);
    }
}
