using System;
using Avalonia.Platform;
using Avalonia.VisualTree;

namespace Avalonia.Rendering
{
    public class DefaultRenderLayerFactory : IRenderLayerFactory
    {
        private IPlatformRenderInterface _renderInterface;

        public DefaultRenderLayerFactory()
            : this(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>())
        {
        }

        public DefaultRenderLayerFactory(IPlatformRenderInterface renderInterface)
        {
            _renderInterface = renderInterface;
        }

        public IRenderTargetBitmapImpl CreateLayer(
            IVisual layerRoot,
            Size size,
            double dpiX,
            double dpiY)
        {
            return _renderInterface.CreateRenderTargetBitmap(
                (int)Math.Ceiling(size.Width),
                (int)Math.Ceiling(size.Height),
                dpiX,
                dpiY);
        }
    }
}
