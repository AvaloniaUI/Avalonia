using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.OpenGL.Imaging
{
    public class OpenGlBitmap : Bitmap, IAffectsRender
    {
        private IOpenGlBitmapImpl _impl;

        public OpenGlBitmap(IOpenGlTextureSharingRenderInterfaceContextFeature feature,
            PixelSize size, Vector dpi) 
            : base(CreateOrThrow(feature, size, dpi))
        {
            _impl = (IOpenGlBitmapImpl)PlatformImpl.Item;
        }

        static IOpenGlBitmapImpl CreateOrThrow(IOpenGlTextureSharingRenderInterfaceContextFeature feature,
            PixelSize size, Vector dpi) => feature.CreateOpenGlBitmap(size, dpi);

        public IOpenGlBitmapAttachment CreateFramebufferAttachment(IGlContext context) =>
            _impl.CreateFramebufferAttachment(context, SetIsDirty);

        public bool SupportsContext(IGlContext context) => _impl.SupportsContext(context);
        
        void SetIsDirty()
        {
            if (Dispatcher.UIThread.CheckAccess())
                CallInvalidated();
            else
                Dispatcher.UIThread.Post(CallInvalidated);
        }

        private void CallInvalidated() => Invalidated?.Invoke(this, EventArgs.Empty);

        public event EventHandler Invalidated;
    }
}
