using Avalonia.Platform;

namespace Avalonia.Direct2D1
{
    internal interface ILayerFactory
    {
        IDrawingContextLayerImpl CreateLayer(Size size);
    }
}
