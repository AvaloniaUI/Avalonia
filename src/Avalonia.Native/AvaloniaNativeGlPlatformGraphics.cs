using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.OpenGL;
using Avalonia.Native.Interop;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Native
{
    class AvaloniaNativeGlPlatformGraphics : IPlatformGraphics
    {
        private readonly IAvnGlDisplay _display;

        public AvaloniaNativeGlPlatformGraphics(IAvnGlDisplay display, IAvaloniaNativeFactory factory)
        {
            _display = display;
            var context = display.CreateContext(null);
            
            int major, minor;
            GlInterface glInterface;
            using (context.MakeCurrent())
            {
                var basic = new GlBasicInfoInterface(display.GetProcAddress);
                basic.GetIntegerv(GlConsts.GL_MAJOR_VERSION, out major);
                basic.GetIntegerv(GlConsts.GL_MINOR_VERSION, out minor);
                basic.GetIntegerv(GlConsts.GL_CONTEXT_PROFILE_MASK, out var profileMask);
                var isCompatibilityProfile = (profileMask & GlConsts.GL_CONTEXT_COMPATIBILITY_PROFILE_BIT) == GlConsts.GL_CONTEXT_COMPATIBILITY_PROFILE_BIT;

                _version = new GlVersion(GlProfileType.OpenGL, major, minor, isCompatibilityProfile);
                glInterface = new GlInterface(_version, (name) =>
                {
                    var rv = _display.GetProcAddress(name);
                    return rv;
                });
            }

            GlDisplay = new GlDisplay(display, glInterface, context.SampleCount, context.StencilSize, factory);
            SharedContext =(GlContext)CreateContext();
        }

        

        public bool UsesSharedContext => true;
        public IPlatformGraphicsContext CreateContext() => new GlContext(GlDisplay,
            null, _display.CreateContext(null), _version);

        public IPlatformGraphicsContext GetSharedContext() => SharedContext;

        public bool CanShareContexts => true;
        public bool CanCreateContexts => true;
        internal GlDisplay GlDisplay;
        private readonly GlVersion _version;
        internal GlContext SharedContext { get; }
    }

    class GlDisplay
    {
        private readonly IAvnGlDisplay _display;

        public GlDisplay(IAvnGlDisplay display, GlInterface glInterface, int sampleCount, int stencilSize,
            IAvaloniaNativeFactory factory)
        {
            _display = display;
            SampleCount = sampleCount;
            StencilSize = stencilSize;
            Factory = factory;
            GlInterface = glInterface;
            MemoryHelper = factory.CreateMemoryManagementHelper();
        }

        public GlInterface GlInterface { get; }

        public int SampleCount { get; }

        public int StencilSize { get; }
        public IAvaloniaNativeFactory Factory { get; }

        public IAvnNativeObjectsMemoryManagement MemoryHelper { get; }

        public void ClearContext() => _display.LegacyClearCurrentContext();

        public GlContext CreateSharedContext(GlContext share) =>
            new GlContext(this, share, _display.CreateContext(share.Context), share.Version);

    }

    class GlContext : IGlContext
    {
        private readonly GlDisplay _display;
        private readonly GlContext? _sharedWith;
        private readonly GpuHandleWrapFeature _handleWrapFeature;
        private readonly GlExternalObjectsFeature _externalObjects;
        public IAvnGlContext? Context { get; private set; }

        public GlContext(GlDisplay display, GlContext? sharedWith, IAvnGlContext context, GlVersion version)
        {
            _display = display;
            _sharedWith = sharedWith;
            Context = context;
            Version = version;
            _handleWrapFeature = new GpuHandleWrapFeature(display.Factory);
            _externalObjects = new GlExternalObjectsFeature(this, display);
        }

        public GlVersion Version { get; }
        public GlInterface GlInterface => _display.GlInterface;
        public int SampleCount => _display.SampleCount;
        public int StencilSize => _display.StencilSize;

        [MemberNotNull(nameof(Context))]
        public void ThrowIfLost()
        {
            if (IsLost)
                throw new PlatformGraphicsContextLostException();
        }

        [MemberNotNull(nameof(Context))]
        public IDisposable MakeCurrent()
        {
            ThrowIfLost();
            return Context.MakeCurrent();
        }

        [MemberNotNullWhen(false, nameof(Context))]
        public bool IsLost => Context == null;

        [MemberNotNull(nameof(Context))]
        public IDisposable EnsureCurrent() => MakeCurrent();

        public bool IsSharedWith(IGlContext context)
        {
            var c = (GlContext)context;
            return c == this
                   || c._sharedWith == this
                   || _sharedWith == context
                   || _sharedWith != null && _sharedWith == c._sharedWith;
        }

        public bool CanCreateSharedContext => true;

        public IGlContext CreateSharedContext(IEnumerable<GlVersion>? preferredVersions = null) =>
            _display.CreateSharedContext(_sharedWith ?? this);

        public void Dispose()
        {
            Context?.Dispose();
            Context = null;
        }

        public object? TryGetFeature(Type featureType)
        {
            if (featureType == typeof(IExternalObjectsHandleWrapRenderInterfaceContextFeature))
                return _handleWrapFeature;
            if (featureType == typeof(IGlContextExternalObjectsFeature))
                return _externalObjects;
            return null;
        }
    }


    class GlPlatformSurfaceRenderTarget : IGlPlatformSurfaceRenderTarget
    {
        private IAvnGlSurfaceRenderTarget? _target;
        private readonly IGlContext _context;

        public GlPlatformSurfaceRenderTarget(IAvnGlSurfaceRenderTarget target, IGlContext context)
        {
            _target = target;
            _context = context;
        }

        public IGlPlatformSurfaceRenderingSession BeginDraw()
        {
            ObjectDisposedException.ThrowIf(_target is null, this);
            return new GlPlatformSurfaceRenderingSession(_context, _target.BeginDrawing());
        }

        public void Dispose()
        {
            _target?.Dispose();
            _target = null;
        }
    }

    class GlPlatformSurfaceRenderingSession : IGlPlatformSurfaceRenderingSession
    {
        private IAvnGlSurfaceRenderingSession? _session;

        public GlPlatformSurfaceRenderingSession(IGlContext context, IAvnGlSurfaceRenderingSession session)
        {
            Context = context;
            _session = session;
        }

        public IGlContext Context { get; }

        private IAvnGlSurfaceRenderingSession Session
        {
            get
            {
                ObjectDisposedException.ThrowIf(_session is null, this);
                return _session;
            }
        }

        public PixelSize Size
        {
            get
            {
                var s = Session.PixelSize;
                return new PixelSize(s.Width, s.Height);
            }
        }

        public double Scaling => Session.Scaling;

        public bool IsYFlipped => true;
        
        public void Dispose()
        {
            _session?.Dispose();
            _session = null;
        }
    }

    class GlPlatformSurface : IGlPlatformSurface
    {
        private readonly IAvnTopLevel _topLevel;
        public GlPlatformSurface(IAvnTopLevel topLevel)
        {
            _topLevel = topLevel;
        }
        
        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            if (!Dispatcher.UIThread.CheckAccess())
                throw new RenderTargetNotReadyException();
            var avnContext = (GlContext)context;
            return new GlPlatformSurfaceRenderTarget(_topLevel.CreateGlRenderTarget(avnContext.Context), avnContext);
        }
    }

    class GlExternalObjectsFeature : IGlContextExternalObjectsFeature
    {
        private readonly GlContext _context;
        private readonly GlDisplay _display;

        public unsafe GlExternalObjectsFeature(GlContext context, GlDisplay display)
        {
            context.ThrowIfLost();

            _context = context;
            _display = display;
            ulong registryId = 0;

            if (context.Context.GetIOKitRegistryId(&registryId) != 0)
            {
                // We are reversing bytes to match MoltenVK (LUID is a Vulkan term after all)
                var bytes = BitConverter.GetBytes(registryId);
                bytes.Reverse();
                DeviceLuid = bytes;
            }
        }

        public IReadOnlyList<string> SupportedImportableExternalImageTypes { get; } =
            [KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef];
        public IReadOnlyList<string> SupportedExportableExternalImageTypes { get; } = [];

        public IReadOnlyList<string> SupportedImportableExternalSemaphoreTypes { get; } =
            [KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent];

        public IReadOnlyList<string> SupportedExportableExternalSemaphoreTypes { get; } = [];
        public IReadOnlyList<PlatformGraphicsExternalImageFormat> GetSupportedFormatsForExternalMemoryType(string type)
        {
            return [PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm];
        }

        public IGlExportableExternalImageTexture CreateImage(string type, PixelSize size, PlatformGraphicsExternalImageFormat format) => 
            throw new NotSupportedException();

        public IGlExportableExternalImageTexture CreateSemaphore(string type) => throw new NotSupportedException();

        public IGlExternalImageTexture ImportImage(IPlatformHandle handle, PlatformGraphicsExternalImageProperties properties)
        {
            if (handle.HandleDescriptor == KnownPlatformGraphicsExternalImageHandleTypes.IOSurfaceRef)
            {
                if (properties.Format != PlatformGraphicsExternalImageFormat.B8G8R8A8UNorm)
                    throw new OpenGlException("Only B8G8R8A8UNorm format is supported for IOSurfaceRef");
                using (_context.EnsureCurrent())
                {
                    _context.GlInterface.GetIntegerv(GlConsts.GL_TEXTURE_BINDING_RECTANGLE, out var oldTexture);
                    var textureId = _context.GlInterface.GenTexture();
                    _context.GlInterface.BindTexture(GlConsts.GL_TEXTURE_RECTANGLE, textureId);
                    var error = _context.Context.texImageIOSurface2D(GlConsts.GL_TEXTURE_RECTANGLE, GlConsts.GL_RGBA8,
                        properties.Width, properties.Height, GlConsts.GL_BGRA, GlConsts.GL_UNSIGNED_INT_8_8_8_8_REV,
                        handle.Handle, 0);
                    //var error = 0;
                    _context.GlInterface.BindTexture(GlConsts.GL_TEXTURE_RECTANGLE, oldTexture);
                    
                    if(error != 0)
                    {
                        _context.GlInterface.DeleteTexture(textureId);
                        throw new OpenGlException("CGLTexImageIOSurface2D returned " + error);
                    }
                    return new ImportedTexture(_context, GlConsts.GL_TEXTURE_RECTANGLE, textureId,
                        GlConsts.GL_RGBA8, properties);
                }
            }
            throw new NotSupportedException("This handle type is not supported");
        }

        public IGlExternalSemaphore ImportSemaphore(IPlatformHandle handle)
        {
            if (handle.HandleDescriptor == KnownPlatformGraphicsExternalSemaphoreHandleTypes.MetalSharedEvent)
            {
                var imported = _display.Factory.ImportMTLSharedEvent(handle.Handle);
                return new MtlEventSemaphore(imported);
            }

            throw new NotSupportedException("This handle type is not supported");
        }

        public CompositionGpuImportedImageSynchronizationCapabilities GetSynchronizationCapabilities(string imageHandleType)
        {
            return CompositionGpuImportedImageSynchronizationCapabilities.Automatic |
                   CompositionGpuImportedImageSynchronizationCapabilities.TimelineSemaphores;
        }

        public byte[]? DeviceLuid { get; }
        public byte[]? DeviceUuid { get; }
    }

    class MtlEventSemaphore(IAvnMTLSharedEvent inner) : IGlExternalSemaphore
    {
        public void WaitSemaphore(IGlExternalImageTexture texture) =>
            throw new NotSupportedException("This is a timeline semaphore");

        public void SignalSemaphore(IGlExternalImageTexture texture) => 
            throw new NotSupportedException("This is a timeline semaphore");

        public void WaitTimelineSemaphore(IGlExternalImageTexture texture, ulong value) =>
            inner.Wait(value, 1000);

        public void SignalTimelineSemaphore(IGlExternalImageTexture texture, ulong value) =>
            inner.SetSignaledValue(value);

        public void Dispose() => inner.Dispose();
    }
    
    class ImportedTexture(GlContext context, int type, int id, int internalFormat,
        PlatformGraphicsExternalImageProperties properties) : IGlExternalImageTexture
    {
        private bool _disposed;
        public void Dispose()
        {
            if(_disposed)
                return;
            try
            {
                _disposed = true;
                using (context.EnsureCurrent()) 
                    context.GlInterface.DeleteTexture(id);
            }
            catch
            {
                // Ignore, context is likely broken
            }
        }

        public void AcquireKeyedMutex(uint key) => throw new NotSupportedException();

        public void ReleaseKeyedMutex(uint key) => throw new NotSupportedException();

        public int TextureId => id;
        public int InternalFormat => internalFormat;
        public int TextureType => type;
        public PlatformGraphicsExternalImageProperties Properties => properties;
    }
}
