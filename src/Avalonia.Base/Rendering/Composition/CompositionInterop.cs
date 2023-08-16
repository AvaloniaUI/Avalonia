using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Rendering.Composition;

internal class CompositionInterop : ICompositionGpuInterop
{
    private readonly Compositor _compositor;
    private readonly IPlatformRenderInterfaceContext _context;
    private readonly IExternalObjectsRenderInterfaceContextFeature _externalObjects;
    

    public CompositionInterop(
        Compositor compositor,
        IExternalObjectsRenderInterfaceContextFeature externalObjects)
    {
        _compositor = compositor;
        _context = compositor.Server.RenderInterface.Value;
        DeviceLuid = externalObjects.DeviceLuid;
        DeviceUuid = externalObjects.DeviceUuid;
        _externalObjects = externalObjects;
    }

    public IReadOnlyList<string> SupportedImageHandleTypes => _externalObjects.SupportedImageHandleTypes;
    public IReadOnlyList<string> SupportedSemaphoreTypes => _externalObjects.SupportedSemaphoreTypes;

    public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
        => _externalObjects.GetSynchronizationCapabilities(imageHandleType);

    public ICompositionImportedGpuImage ImportImage(IPlatformHandle handle,
        PlatformGraphicsExternalImageProperties properties)
        => new CompositionImportedGpuImage(_compositor, _context, _externalObjects, 
            () => _externalObjects.ImportImage(handle, properties));

    public ICompositionImportedGpuImage ImportImage(ICompositionImportableSharedGpuContextImage image)
    {
        return new CompositionImportedGpuImage(_compositor, _context, _externalObjects,
            () => _externalObjects.ImportImage(image));
    }

    public ICompositionImportedGpuSemaphore ImportSemaphore(IPlatformHandle handle)
        => new CompositionImportedGpuSemaphore(handle, _compositor, _context, _externalObjects);

    public ICompositionImportedGpuImage ImportSemaphore(ICompositionImportableSharedGpuContextSemaphore image)
    {
        throw new System.NotSupportedException();
    }

    public bool IsLost { get; }
    public byte[]? DeviceLuid { get; set; }
    public byte[]? DeviceUuid { get; set; }
}

abstract class CompositionGpuImportedObjectBase : ICompositionGpuImportedObject
{
    protected Compositor Compositor { get; }
    public IPlatformRenderInterfaceContext Context { get; }
    public IExternalObjectsRenderInterfaceContextFeature Feature { get; }

    public CompositionGpuImportedObjectBase(Compositor compositor,
        IPlatformRenderInterfaceContext context,
        IExternalObjectsRenderInterfaceContextFeature feature)
    {
        Compositor = compositor;
        Context = context;
        Feature = feature;
        
        ImportCompleted = Compositor.InvokeServerJobAsync(Import);
    }
    
    protected abstract void Import();
    public abstract void Dispose();

    public Task ImportCompleted { get; }

    public Task ImportCompeted => ImportCompleted;
    public bool IsLost => Context.IsLost;

    public ValueTask DisposeAsync() => new(Compositor.InvokeServerJobAsync(() =>
    {
        if (ImportCompleted.Status == TaskStatus.RanToCompletion)
            Dispose();
    }));
}

class CompositionImportedGpuImage : CompositionGpuImportedObjectBase, ICompositionImportedGpuImage
{
    private readonly Func<IPlatformRenderInterfaceImportedImage> _importer;
    private IPlatformRenderInterfaceImportedImage? _image;

    public CompositionImportedGpuImage(Compositor compositor,
        IPlatformRenderInterfaceContext context,
        IExternalObjectsRenderInterfaceContextFeature feature,
        Func<IPlatformRenderInterfaceImportedImage> importer): base(compositor, context, feature)
    {
        _importer = importer;
    }

    protected override void Import()
    {
        using (Compositor.Server.RenderInterface.EnsureCurrent())
        {
            // The original context was lost and the new one might have different capabilities
            if (Context != Compositor.Server.RenderInterface.Value)
                throw new PlatformGraphicsContextLostException();
            _image = _importer();
        }
    }

    public IPlatformRenderInterfaceImportedImage Image =>
        _image ?? throw new ObjectDisposedException(nameof(CompositionImportedGpuImage));

    public bool IsUsable => _image != null && Compositor.Server.RenderInterface.Value == Context;

    public override void Dispose()
    {
        _image?.Dispose();
        _image = null!;
    }
}

class CompositionImportedGpuSemaphore : CompositionGpuImportedObjectBase, ICompositionImportedGpuSemaphore
{
    private readonly IPlatformHandle _handle;
    private IPlatformRenderInterfaceImportedSemaphore? _semaphore;

    public CompositionImportedGpuSemaphore(IPlatformHandle handle,
        Compositor compositor, IPlatformRenderInterfaceContext context,
        IExternalObjectsRenderInterfaceContextFeature feature) : base(compositor, context, feature)
    {
        _handle = handle;
    }

    public IPlatformRenderInterfaceImportedSemaphore Semaphore =>
        _semaphore ?? throw new ObjectDisposedException(nameof(CompositionImportedGpuSemaphore));


    public bool IsUsable => _semaphore != null && Compositor.Server.RenderInterface.Value == Context;

    protected override void Import()
    {
        _semaphore = Feature.ImportSemaphore(_handle);
    }

    public override void Dispose()
    {
        _semaphore?.Dispose();
        _semaphore = null;
    }
}
