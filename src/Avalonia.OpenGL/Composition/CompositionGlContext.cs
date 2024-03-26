using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering.Composition;

namespace Avalonia.OpenGL.Composition;

abstract class CompositionGlContextBase : ICompositionGlContext
{
    protected readonly ICompositionGpuInterop Interop;
    public Compositor Compositor { get; }
    public IGlContext Context { get; }
    private List<CompositionGlSwapchain> _swapchains = new();

    public abstract ICompositionGlSwapchain CreateSwapchain(CompositionDrawingSurface surface, PixelSize size,
        Action? onDispose = null);
    
    internal CompositionGlContextBase(
        Compositor compositor,
        IGlContext context,
        ICompositionGpuInterop interop)
    {
        Compositor = compositor;
        Interop = interop;
        Context = context;
    }

    public ValueTask DisposeAsync()
    {
        if (Compositor.Dispatcher.CheckAccess())
            return DisposeAsyncCore();
        return new ValueTask(Compositor.Dispatcher.InvokeAsync(() => DisposeAsyncCore().AsTask()));
    }

    private async ValueTask DisposeAsyncCore()
    {
        while (_swapchains.Count > 0)
        {
            var chain = _swapchains[_swapchains.Count - 1];
            // The swapchain will remove itself
            await chain.DisposeAsync();
        }
        Context.Dispose();
    }

    internal void RemoveSwapchain(CompositionGlSwapchain chain)
    {
        Compositor.Dispatcher.VerifyAccess();
        _swapchains.Remove(chain);
    }

    internal void AddSwapchain(CompositionGlSwapchain chain)
    {
        Compositor.Dispatcher.VerifyAccess();
        _swapchains.Add(chain);
    }
}


class CompositionGlContextViaContextSharing : CompositionGlContextBase
{
    private readonly IOpenGlTextureSharingRenderInterfaceContextFeature _sharing;

    public CompositionGlContextViaContextSharing(
        Compositor compositor,
        IGlContext context,
        ICompositionGpuInterop interop,
        IOpenGlTextureSharingRenderInterfaceContextFeature sharing) : base(compositor, context, interop)
    {
        _sharing = sharing;
    }

    public override ICompositionGlSwapchain CreateSwapchain(CompositionDrawingSurface surface, PixelSize size,
        Action? onDispose)
    {
        Compositor.Dispatcher.VerifyAccess();
        return new CompositionGlSwapchain(this, surface, Interop,
            (imageSize, surface) => new CompositionOpenGlSwapChainImage(Context, _sharing, imageSize, Interop, surface),
            size, 2, onDispose);
    }
}

class CompositionGlContextViaExternalObjects : CompositionGlContextBase
{
    private readonly IGlContextExternalObjectsFeature _externalObjectsFeature;
    
    // ReSharper disable once NotAccessedField.Local
    // TODO: Implement
    private readonly string _externalImageType;
    // ReSharper disable once NotAccessedField.Local
    // TODO: Implement
    private readonly string? _externalSemaphoreType;
    
    private readonly CompositionGpuImportedImageSynchronizationCapabilities _syncMode;


    public CompositionGlContextViaExternalObjects(Compositor compositor, IGlContext context,
        ICompositionGpuInterop interop, IGlContextExternalObjectsFeature externalObjectsFeature,
        string externalImageType, CompositionGpuImportedImageSynchronizationCapabilities syncMode,
        string? externalSemaphoreType) : base(compositor, context, interop)
    {
        _externalObjectsFeature = externalObjectsFeature;
        _externalImageType = externalImageType;
        _syncMode = syncMode;
        _externalSemaphoreType = externalSemaphoreType;
        if (_syncMode != CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex)
            throw new NotSupportedException("Only IDXGIKeyedMutex sync is supported for non-shared contexts");
    }

    public override ICompositionGlSwapchain CreateSwapchain(CompositionDrawingSurface surface, PixelSize size, Action? onDispose)
    {
        Compositor.Dispatcher.VerifyAccess();
        if (_syncMode == CompositionGpuImportedImageSynchronizationCapabilities.KeyedMutex)

            return new CompositionGlSwapchain(this, surface, Interop,
                (imageSize, surface) =>
                    new DxgiMutexOpenGlSwapChainImage(Interop, surface, _externalObjectsFeature, imageSize),
                size, 2, onDispose);

        throw new System.NotSupportedException();
    }
}
