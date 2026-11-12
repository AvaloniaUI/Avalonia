using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Platform.Surfaces;
using NImpeller;

namespace Avalonia.Impeller;

/// <summary>An Impeller context for one Avalonia graphics context, with resources shared by its frames.</summary>
internal abstract class ImpellerBackendContext : IPlatformRenderInterfaceContext
{
    private ImpellerContext? _impellerContext;
    private readonly Dictionary<Type, IDisposable> _extensions = new();
    private List<IDisposable> _thisFrameResources = new();
    private List<IDisposable> _lastFrameResources = new();

    protected ImpellerBackendContext(ImpellerContext impellerContext)
    {
        _impellerContext = impellerContext;
        RenderLayerPool = new ImpellerRenderLayerPool(this);
    }

    internal ImpellerContext ImpellerContext =>
        _impellerContext ?? throw new ObjectDisposedException(nameof(ImpellerBackendContext));

    internal bool IsDisposed => _impellerContext is null;

    /// <summary>Returns this context's object of type <typeparamref name="T"/>, created on first use.</summary>
    internal T GetExtension<T>(Func<T> create) where T : class, IDisposable
    {
        if (!_extensions.TryGetValue(typeof(T), out var extension))
            _extensions[typeof(T)] = extension = create();
        return (T)extension;
    }

    /// <summary>Intermediate layers for tile and scene brushes.</summary>
    internal ImpellerRenderLayerPool RenderLayerPool { get; }

    internal ImpellerDashedOutlineCache DashedOutlines { get; } = new();

    /// <summary>Creates a layer that can be sampled as a texture.</summary>
    internal abstract IImpellerOffscreenLayer CreateTextureLayer(PixelSize size, Vector dpi);

    public virtual IDrawingContextLayerImpl CreateOffscreenRenderTarget(PixelSize pixelSize, Vector scaling,
        bool enableTextAntialiasing) => CreateTextureLayer(pixelSize, scaling * 96);

    /// <summary>Wraps native work such as Metal autorelease pools.</summary>
    internal virtual IDisposable? EnterNativeScope() => null;

    internal void AddFrameResources(List<IDisposable> resources) => _thisFrameResources.AddRange(resources);

    internal void RetireFrameResources()
    {
        foreach (var resource in _lastFrameResources)
            resource.Dispose();
        _lastFrameResources = _thisFrameResources;
        _thisFrameResources = new List<IDisposable>();
    }

    /// <summary>Called after a frame is presented.</summary>
    internal void EndFrame()
    {
        RetireFrameResources();
        RenderLayerPool.EndFrame();
        DashedOutlines.EndFrame();
    }

    public abstract IRenderTarget CreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);

    public abstract bool IsReadyToCreateRenderTarget(IEnumerable<IPlatformRenderSurface> surfaces);

    public abstract bool IsLost { get; }

    public PixelSize? MaxOffscreenRenderTargetPixelSize => null;

    public virtual IReadOnlyDictionary<Type, object> PublicFeatures { get; } = new Dictionary<Type, object>();

    public object? TryGetFeature(Type featureType) =>
        PublicFeatures.TryGetValue(featureType, out var feature) ? feature : null;

    public virtual void Dispose()
    {
        foreach (var resource in _lastFrameResources)
            resource.Dispose();
        foreach (var resource in _thisFrameResources)
            resource.Dispose();
        _lastFrameResources.Clear();
        _thisFrameResources.Clear();
        RenderLayerPool.Dispose();
        DashedOutlines.Dispose();
        foreach (var extension in _extensions.Values)
            extension.Dispose();
        _extensions.Clear();
        _impellerContext?.Dispose();
        _impellerContext = null;
    }
}
