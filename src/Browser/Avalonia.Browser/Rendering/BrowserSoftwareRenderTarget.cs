using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform.Surfaces;
using Avalonia.Platform;
using Avalonia.Reactive;
#pragma warning disable CS0169
#pragma warning disable CA1823

namespace Avalonia.Browser.Rendering;

partial class BrowserSoftwareRenderTarget : BrowserRenderTarget, IFramebufferPlatformSurface
{
    private readonly Func<(PixelSize, double)> _sizeGetter;
    public override IPlatformGraphicsContext? PlatformGraphicsContext => null;
    private Action<RetainedFramebuffer> _blit;
    
    public BrowserSoftwareRenderTarget(JSObject js, Func<(PixelSize, double)> sizeGetter) : base(js)
    {
        _sizeGetter = sizeGetter;
        _blit = Blit;
    }


    class FramebufferRenderTarget : IFramebufferRenderTarget
    {
        private readonly BrowserSoftwareRenderTarget _parent;
        private RetainedFramebuffer? _fb;

        public FramebufferRenderTarget(BrowserSoftwareRenderTarget parent)
        {
            _parent = parent;
        }
        
        public void Dispose()
        {
            _fb?.Dispose();
            _fb = null;
        }

        public ILockedFramebuffer Lock()
        {
            var (size, scaling) = _parent._sizeGetter();
            _parent.UpdateSize(size);
            
            if (_fb == null || _fb.Size != size)
            {
                _fb?.Dispose();
                _fb = null;
                _fb = new RetainedFramebuffer(size, PixelFormat.Rgba8888);
            }

            return _fb.Lock(new Vector(scaling * 96, scaling * 96), _parent._blit);
        }
    }
    
    public IFramebufferRenderTarget CreateFramebufferRenderTarget()
    {
        return new FramebufferRenderTarget(this);
    }

    [JSImport("SoftwareRenderTarget.staticPutPixelData", AvaloniaModule.MainModuleName)]
    public static partial void PutPixelData(JSObject js, int address, int size, int width, int height);
    
    private void Blit(RetainedFramebuffer fb)
    {
        PutPixelData(Js, fb.Address.ToInt32(), fb.Size.Width * fb.Size.Height * 4, fb.Size.Width, fb.Size.Height);
    }
}