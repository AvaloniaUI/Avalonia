using System;
using System.Numerics;
using Avalonia.MicroCom;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.WinRT.Composition
{
    public class WinUICompositedWindow : IDisposable
    {
        private EglContext _syncContext;
        private readonly IVisual _blurVisual;
        private ICompositionTarget _compositionTarget;
        private IVisual _contentVisual;
        private ICompositionDrawingSurfaceInterop _surfaceInterop;
        private PixelSize _size;

        private static Guid IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
        

        internal WinUICompositedWindow(EglContext syncContext,
            ICompositionTarget compositionTarget,
            ICompositionDrawingSurfaceInterop surfaceInterop,
            IVisual contentVisual, IVisual blurVisual)
        {
            _syncContext = syncContext;
            _blurVisual = blurVisual.CloneReference();
            _compositionTarget = compositionTarget.CloneReference();
            _contentVisual = contentVisual.CloneReference();
            _surfaceInterop = surfaceInterop.CloneReference();
        }


        public void ResizeIfNeeded(PixelSize size)
        {
            using (_syncContext.EnsureLocked())
            {
                if (_size != size)
                {
                    _surfaceInterop.Resize(new UnmanagedMethods.POINT { X = size.Width, Y = size.Height });
                    _contentVisual.SetSize(new Vector2(size.Width, size.Height));
                    _size = size;
                }
            }
        }

        public unsafe IUnknown BeginDrawToTexture(out PixelPoint offset)
        {
            if (!_syncContext.IsCurrent)
                throw new InvalidOperationException();
            
            var iid = IID_ID3D11Texture2D;
            void* pTexture;
            var off = _surfaceInterop.BeginDraw(null, &iid, &pTexture);
            offset = new PixelPoint(off.X, off.Y);
            return MicroComRuntime.CreateProxyFor<IUnknown>(pTexture, true);
        }

        public void EndDraw()
        {
            if (!_syncContext.IsCurrent)
                throw new InvalidOperationException();
            _surfaceInterop.EndDraw();
        }

        public void SetBlur(bool enable)
        {
            using (_syncContext.EnsureLocked())
                _blurVisual.SetIsVisible(enable ? 1 : 0);
        }

        public void Dispose()
        {
            if (_syncContext == null)
            {
                _blurVisual.Dispose();
                _contentVisual.Dispose();
                _surfaceInterop.Dispose();
                _compositionTarget.Dispose();
            }
        }
    }
}
