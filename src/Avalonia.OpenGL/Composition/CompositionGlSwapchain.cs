using System;
using System.Threading.Tasks;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.OpenGL.Composition;

class CompositionGlSwapchain : AsyncSwapchainBase<IGlSwapchainImage>, ICompositionGlSwapchain
{
    protected readonly IGlContext Context;
    private readonly Func<PixelSize, CompositionDrawingSurface, IGlSwapchainImage> _imageFactory;
    private readonly Action? _onDispose;
    private readonly CompositionDrawingSurface _surface;
    private readonly Dispatcher _dispatcher;
    private readonly CompositionGlContextBase _parent;

    public CompositionGlSwapchain(
        CompositionGlContextBase parentContext,
        CompositionDrawingSurface surface,
        ICompositionGpuInterop interop,
        Func<PixelSize, CompositionDrawingSurface, IGlSwapchainImage> imageFactory,
        PixelSize size, int queueLength, Action? onDispose) : base(interop, surface, size, queueLength, "OpenGL")
    {
        _parent = parentContext;
        Context = parentContext.Context;
        _imageFactory = imageFactory;
        _onDispose = onDispose;
        _surface = surface;
        _dispatcher = _surface.Compositor.Dispatcher;
        _parent.AddSwapchain(this);
    }


    public CompositionSurface Surface => _surface;

    class LockedTexture : ICompositionGlSwapchainLockedTexture
    {
        private  IDisposable? _disposable;

        public Task Presented { get; }
        public int TextureId { get; private set; }

        public LockedTexture((IDisposable disposable, IGlSwapchainImage texture, Task presented) res)
        {
            TextureId = res.texture.TextureId;
            _disposable = res.disposable;
            Presented = res.presented;
        }

        public void Dispose()
        {
            _disposable?.Dispose();
            _disposable = null;
            TextureId = 0;
        }

    }
    
    ICompositionGlSwapchainLockedTexture? TryGetNextTextureCore()
    {
        var res = TryBeginDraw();
        if (res != null)
            return new LockedTexture(res.Value);
        return null;
    }
    
    public ICompositionGlSwapchainLockedTexture? TryGetNextTexture()
    {
        _dispatcher.VerifyAccess();
        using (Context.EnsureCurrent())
            return TryGetNextTextureCore();
    }

    public async ValueTask<ICompositionGlSwapchainLockedTexture> GetNextTextureAsync()
    {
        _dispatcher.VerifyAccess();
        if (Context is IGlContextWithIsCurrentCheck currentCheck && currentCheck.IsCurrent)
            throw new InvalidOperationException(
                "You should not be calling _any_ asynchronous methods inside of MakeCurrent/EnsureCurrent blocks. Awaiting such method will result in a broken opengl state");

        var res = await BeginDrawAsync();
        return new LockedTexture(res);
    }

    public ICompositionGlSwapchainLockedTexture GetNextTextureIgnoringQueueLimits()
    {
        _dispatcher.VerifyAccess();
        using (Context.EnsureCurrent())
        {
            return new LockedTexture(base.BeginDraw());
        }
    }


    protected override IGlSwapchainImage CreateImage(PixelSize size)
    {
        using (Context.EnsureCurrent())
            return _imageFactory(size, _surface);
    }

    protected override async ValueTask DisposeImage(IGlSwapchainImage image)
    {
        await image.DisposeImportedAsync();
        using (Context.EnsureCurrent())
            image.DisposeTexture();
    }

    protected override Task PresentImage(IGlSwapchainImage image)
    {
        _dispatcher.VerifyAccess();
        using (Context.EnsureCurrent())
        {
            Context.GlInterface.Flush();
            return image.Present();
        }
    }

    protected override void BeginDraw(IGlSwapchainImage image)
    {
        using (Context.EnsureCurrent())
            image.BeginDraw();
    }

    public override ValueTask DisposeAsync()
    {
        if (!_dispatcher.CheckAccess())
            return new ValueTask(_dispatcher.InvokeAsync(() => DisposeAsyncCore().AsTask()));
        return DisposeAsyncCore();
    }

    private async ValueTask DisposeAsyncCore()
    {
        _onDispose?.Invoke();
        await base.DisposeAsync();
        _parent.RemoveSwapchain(this);
    }
}