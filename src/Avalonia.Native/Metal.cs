using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metal;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Utilities;

namespace Avalonia.Native;

class MetalPlatformGraphics : IPlatformGraphics
{
    private readonly IAvaloniaNativeFactory _factory;
    private readonly IAvnMetalDisplay _display;

    public MetalPlatformGraphics(IAvaloniaNativeFactory factory)
    {
        _factory = factory;
        _display = factory.ObtainMetalDisplay();
    }
    public bool UsesSharedContext => false;
    public IPlatformGraphicsContext CreateContext() => new MetalDevice(_factory, _display.CreateDevice());

    public IPlatformGraphicsContext GetSharedContext() => throw new NotSupportedException();
}

class MetalDevice : IMetalDevice
{
    public IAvnMetalDevice Native { get; private set; }
    private DisposableLock _syncRoot = new();
    private readonly GpuHandleWrapFeature _handleWrapFeature;
    private readonly MetalExternalObjectsFeature _externalObjectsFeature;


    public MetalDevice(IAvaloniaNativeFactory factory, IAvnMetalDevice native)
    {
        Native = native;
        _handleWrapFeature = new GpuHandleWrapFeature(factory);
        _externalObjectsFeature = new MetalExternalObjectsFeature(native);
    }

    public void Dispose()
    {
        Native?.Dispose();
        Native = null;
    }

    public object TryGetFeature(Type featureType)
    {
        if (featureType == typeof(IExternalObjectsHandleWrapRenderInterfaceContextFeature))
            return _handleWrapFeature;
        if (featureType == typeof(IMetalExternalObjectsFeature))
            return _externalObjectsFeature;
        return null;
    }

    public bool IsLost => false;

    public IDisposable EnsureCurrent() => _syncRoot.Lock();

    public IntPtr Device => Native.Device;
    public IntPtr CommandQueue => Native.Queue;
}

class MetalPlatformSurface : IMetalPlatformSurface
{
    private readonly IAvnTopLevel _topLevel;

    public MetalPlatformSurface(IAvnTopLevel topLevel)
    {
        _topLevel = topLevel;
    }
    public IMetalPlatformSurfaceRenderTarget CreateMetalRenderTarget(IMetalDevice device)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            throw new RenderTargetNotReadyException();
        
        var dev = (MetalDevice)device;
        var target = _topLevel.CreateMetalRenderTarget(dev.Native);
        return new MetalRenderTarget(target);
    }
}

internal class MetalExternalObjectsFeature : IMetalExternalObjectsFeature
{
    private readonly IAvnMetalDevice _device;

    public unsafe MetalExternalObjectsFeature(IAvnMetalDevice device)
    {
        _device = device;
        ulong registryId;
        if (_device.GetIOKitRegistryId(&registryId) != 0)
        {
            var bytes = BitConverter.GetBytes(registryId);
            bytes.Reverse();
            DeviceLuid = bytes;
        }
    }

    public IReadOnlyList<string> SupportedImageHandleTypes { get; } =
        [KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef];

    public IReadOnlyList<string> SupportedSemaphoreTypes { get; } =
        [KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent];
    
    public byte[] DeviceLuid { get; }

    public CompositionGpuImportedImageSynchronizationCapabilities
        GetSynchronizationCapabilities(string imageHandleType) =>
        CompositionGpuImportedImageSynchronizationCapabilities.TimelineSemaphores;

    public IMetalExternalTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
    {
        var format = properties.Format switch
        {
            PlatformGraphicsExternalImageFormat.R8G8B8A8UNorm => AvnPixelFormat.kAvnRgba8888,
            PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm => AvnPixelFormat.kAvnBgra8888,
            _ => throw new NotSupportedException("Pixel format is not supported")
        };
        
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef)
            throw new NotSupportedException();

        return new ImportedTexture(_device.ImportIOSurface(handle.Handle, format));
    }

    public IMetalSharedEvent ImportSharedEvent(IPlatformHandle handle)
    {
        if (handle.HandleDescriptor != KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent)
            throw new NotSupportedException();
        return new SharedEvent(_device.ImportSharedEvent(handle.Handle));
    }

    class ImportedTexture(IAvnMetalTexture texture) : IMetalExternalTexture
    {
        public void Dispose() => texture.Dispose();

        public int Width => texture.Width;

        public int Height => texture.Height;

        public int Samples => texture.SampleCount;

        public IntPtr Handle => texture.NativeHandle;
    }
    
    class SharedEvent(IAvnMTLSharedEvent inner) : IMetalSharedEvent
    {
        public IAvnMTLSharedEvent Native => inner;
        public void Dispose()
        {
            inner.Dispose();
        }

        public IntPtr Handle => inner.NativeHandle;
    }

    public void SubmitWait(IMetalSharedEvent @event, ulong waitForValue) =>
        _device.SubmitWait(((SharedEvent)@event).Native, waitForValue);

    public void SubmitSignal(IMetalSharedEvent @event, ulong signalValue) =>
        _device.SubmitSignal(((SharedEvent)@event).Native, signalValue);
}

internal class MetalRenderTarget : IMetalPlatformSurfaceRenderTarget
{
    private IAvnMetalRenderTarget _native;

    public MetalRenderTarget(IAvnMetalRenderTarget native)
    {
        _native = native;
    }

    public void Dispose()
    {
        _native?.Dispose();
        _native = null;
    }

    public IMetalPlatformSurfaceRenderingSession BeginRendering()
    {
        var session = _native.BeginDrawing();
        return new MetalDrawingSession(session);
    }
}

internal class MetalDrawingSession : IMetalPlatformSurfaceRenderingSession
{
    private IAvnMetalRenderingSession _session;

    public MetalDrawingSession(IAvnMetalRenderingSession session)
    {
        _session = session;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _session = null;
    }

    public IntPtr Texture => _session.Texture;
    public PixelSize Size
    {
        get
        {
            var size = _session.PixelSize;
            return new(size.Width, size.Height);
        }
    }

    public double Scaling => _session.Scaling;
    public bool IsYFlipped => false;
}
