using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.OpenGL.Imaging
{
    public class OpenGlTextureBitmap : Bitmap, IAffectsRender
    {
        private IOpenGlTextureBitmapImpl _impl;
        static IOpenGlTextureBitmapImpl CreateOrThrow()
        {
            if (!(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>() is IOpenGlAwarePlatformRenderInterface
                glAware))
                throw new PlatformNotSupportedException("Rendering platform does not support OpenGL integration");
            return glAware.CreateOpenGlTextureBitmap();
        }
        
        public OpenGlTextureBitmap() 
            : base(CreateOrThrow())
        {
            _impl = (IOpenGlTextureBitmapImpl)PlatformImpl.Item;
        }

        public IDisposable Lock() => _impl.Lock();

        public void SetTexture(int textureId, int internalFormat, PixelSize size, double dpiScaling)
        {
            _impl.SetBackBuffer(textureId, internalFormat, size, dpiScaling);
            SetIsDirty();
        }

        public void SetIsDirty()
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
