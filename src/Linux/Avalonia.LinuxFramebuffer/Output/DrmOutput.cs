using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Utilities;
using static Avalonia.LinuxFramebuffer.NativeUnsafeMethods;
using static Avalonia.LinuxFramebuffer.Output.LibDrm;

namespace Avalonia.LinuxFramebuffer.Output
{
    public unsafe class DrmOutput : IGlOutputBackend, IGlPlatformSurface, ISurfaceOrientation
    {
        private DrmOutputOptions _outputOptions = new();
        private DrmCard _card;
        public PixelSize PixelSize => Orientation == SurfaceOrientation.Rotation0 || Orientation == SurfaceOrientation.Rotation180
            ? new PixelSize(_mode.Resolution.Width, _mode.Resolution.Height)
            : new PixelSize(_mode.Resolution.Height, _mode.Resolution.Width);

        public double Scaling
        {
            get => _outputOptions.Scaling;
            set => _outputOptions.Scaling = value;
        }

        public SurfaceOrientation Orientation
        {
            get => _outputOptions.Orientation;
            set => _outputOptions.Orientation = value;
        }

        class SharedContextGraphics : IPlatformGraphics
        {
            private readonly IPlatformGraphicsContext _context;

            public SharedContextGraphics(IPlatformGraphicsContext context)
            {
                _context = context;
            }
            public bool UsesSharedContext => true;
            public IPlatformGraphicsContext CreateContext() => throw new NotSupportedException();

            public IPlatformGraphicsContext GetSharedContext() => _context;
        }

        public IPlatformGraphics PlatformGraphics { get; private set; }

        public DrmOutput(DrmCard card, DrmResources resources, DrmConnector connector, DrmModeInfo modeInfo,
            DrmOutputOptions? options = null)
        {
            if (options != null)
                _outputOptions = options;
            Init(card, resources, connector, modeInfo);
        }

        public DrmOutput(string? path = null, bool connectorsForceProbe = false, DrmOutputOptions? options = null)
            :this(new DrmCard(path), connectorsForceProbe, options)
        {
        }

        public DrmOutput(DrmCard card, bool connectorsForceProbe = false, DrmOutputOptions? options = null)
        {
            if (options != null)
                _outputOptions = options;

            var resources = card.GetResources(connectorsForceProbe);

            IEnumerable<DrmConnector> connectors = resources.Connectors;

            if (options?.ConnectorType is { } connectorType)
            {
                connectors = connectors.Where(c => c.ConnectorType == connectorType);
            }

            if (options?.ConnectorTypeId is { } connectorTypeId)
            {
                connectors = connectors.Where(c => c.ConnectorTypeId == connectorTypeId);
            }

            var connector =
                connectors.FirstOrDefault(x => x.Connection == DrmModeConnection.DRM_MODE_CONNECTED);

            if (connector == null)
                throw new InvalidOperationException("Unable to find connected DRM connector");

            DrmModeInfo? mode = null;

            if (options?.VideoMode != null)
            {
                mode = connector.Modes
                    .FirstOrDefault(x => x.Resolution.Width == options.VideoMode.Value.Width &&
                                         x.Resolution.Height == options.VideoMode.Value.Height);
            }

            mode ??= connector.Modes.OrderByDescending(x => x.IsPreferred)
                .ThenByDescending(x => x.Resolution.Width * x.Resolution.Height)
                //.OrderByDescending(x => x.Resolution.Width * x.Resolution.Height)
                .FirstOrDefault();
            if (mode == null)
                throw new InvalidOperationException("Unable to find a usable DRM mode");
            Init(card, resources, connector, mode);
        }

        public DrmOutput(DrmCard card, DrmResources resources, DrmConnector connector, DrmModeInfo modeInfo)
        {
            Init(card, resources, connector, modeInfo);
        }

        [DllImport("libEGL.so.1")]
        static extern IntPtr eglGetProcAddress(string proc);

        private drmModeModeInfo _mode;
        private IntPtr _gbmDevice;
        private EglDisplay _eglDisplay;
        private EglContext _deferredContext;
        private IRef<GbmEglSwapchain> _renderSwapchain;
        private GbmEglSwapchain.FrontBuffer? _scanoutBuffer;
        private GbmEglSwapchain.FrontBuffer? _pendingBuffer;
        private GCHandle _pendingFlipHandle;
        private volatile bool _pageFlipCompleted;
        private uint _crtcId;

        private static readonly DrmEventPageFlipHandlerDelegate s_pageFlipHandler =
            (_, _, _, _, userData) =>
            {
                var output = (DrmOutput)GCHandle.FromIntPtr((IntPtr)userData).Target!;
                output._pageFlipCompleted = true;
            };

        [MemberNotNull(nameof(_card))]
        [MemberNotNull(nameof(PlatformGraphics))]
        [MemberNotNull(nameof(_eglDisplay))]
        [MemberNotNull(nameof(_renderSwapchain))]
        [MemberNotNull(nameof(_deferredContext))]
        void Init(DrmCard card, DrmResources resources, DrmConnector connector, DrmModeInfo modeInfo)
        {
            _card = card;
            uint GetCrtc()
            {
                if (resources.Encoders.TryGetValue(connector.EncoderId, out var encoder))
                {
                    // Not sure why that should work
                    return encoder.Encoder.crtc_id;
                }
                else
                {
                    foreach (var encId in connector.EncoderIds)
                    {
                        if (resources.Encoders.TryGetValue(encId, out encoder)
                            && encoder.PossibleCrtcs.Count > 0)
                            return encoder.PossibleCrtcs.First().crtc_id;
                    }

                    throw new InvalidOperationException("Unable to find CRTC matching the desired mode");
                }
            }

            _crtcId = GetCrtc();
            _gbmDevice = gbm_create_device(card.Fd);
            if (_gbmDevice == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to create GBM device");

            _eglDisplay = new EglDisplay(
                new EglDisplayCreationOptions
                {
                    Egl = new EglInterface(eglGetProcAddress),
                    PlatformType = 0x31D7,
                    PlatformDisplay = _gbmDevice,
                    SupportsMultipleContexts = true,
                    SupportsContextSharing = true
                });

            _deferredContext = _eglDisplay.CreateContext(null);
            PlatformGraphics = new SharedContextGraphics(_deferredContext);
            _renderSwapchain = RefCountable.Create(
                new GbmEglSwapchain(_card, _gbmDevice, _eglDisplay, modeInfo.Resolution));

            var initialBufferSwappingColorR = _outputOptions.InitialBufferSwappingColor.R / 255.0f;
            var initialBufferSwappingColorG = _outputOptions.InitialBufferSwappingColor.G / 255.0f;
            var initialBufferSwappingColorB = _outputOptions.InitialBufferSwappingColor.B / 255.0f;
            var initialBufferSwappingColorA = _outputOptions.InitialBufferSwappingColor.A / 255.0f;
            using (_deferredContext.MakeCurrent(_renderSwapchain.Item.EglSurface))
            {
                _deferredContext.GlInterface.ClearColor(initialBufferSwappingColorR, initialBufferSwappingColorG,
                    initialBufferSwappingColorB, initialBufferSwappingColorA);
                _deferredContext.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_STENCIL_BUFFER_BIT);
                _renderSwapchain.Item.EglSurface.SwapBuffers();
            }

            var buffer = GbmEglSwapchain.LockFrontBuffer(_renderSwapchain);
            var connectorId = connector.Id;
            var mode = modeInfo.Mode;
            try
            {
                var result = drmModeSetCrtc(_card.Fd, _crtcId, buffer.FramebufferId,
                    0, 0, &connectorId, 1, &mode);
                if (result != 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "drmModeSetCrtc failed");
                _scanoutBuffer = buffer;
            }
            catch
            {
                buffer.Dispose();
                throw;
            }
            _mode = mode;

            if (_outputOptions.EnableInitialBufferSwapping)
            {
                //Go through two cycles of buffer swapping (there are render artifacts otherwise)
                using var renderTarget = CreateGlRenderTarget();
                for (var c = 0; c < 2; c++)
                    using (var session = renderTarget.BeginDraw(new IRenderTarget.RenderTargetSceneInfo(PixelSize, 1, CompositionTransparencyLevel.None)))
                    {
                        _deferredContext.GlInterface.ClearColor(initialBufferSwappingColorR, initialBufferSwappingColorG,
                            initialBufferSwappingColorB, initialBufferSwappingColorA);
                        _deferredContext.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_STENCIL_BUFFER_BIT);
                        (session as IGlPlatformSurfaceRenderingSessionWithPartialPresentation)?.PartialPresentation?.Succeeded();
                    }
            }

        }

        private void PageFlip(IRef<GbmEglSwapchain> swapchain)
        {
            var next = GbmEglSwapchain.LockFrontBuffer(swapchain);
            var handle = default(GCHandle);
            var submitted = false;
            try
            {
                // Wait for the previous flip after acquiring the newly rendered frame.
                WaitForPageFlip();
                _pageFlipCompleted = false;
                handle = GCHandle.Alloc(this);
                if (drmModePageFlip(_card.Fd, _crtcId, next.FramebufferId,
                        DrmModePageFlip.Event, (void*)GCHandle.ToIntPtr(handle)) != 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "drmModePageFlip failed");

                _pendingBuffer = next;
                _pendingFlipHandle = handle;
                submitted = true;
            }
            finally
            {
                if (!submitted)
                {
                    if (handle.IsAllocated)
                        handle.Free();
                    next.Dispose();
                }
            }
            // Render ahead while the queued flip waits for vblank, unless the pool is exhausted.
            if (!swapchain.Item.HasFreeBuffers)
                WaitForPageFlip();
        }

        private void WaitForPageFlip()
        {
            if (_pendingBuffer == null)
                return;

            var context = new DrmEventContext
            {
                version = 4,
                page_flip_handler = Marshal.GetFunctionPointerForDelegate(s_pageFlipHandler)
            };
            // Keep both leases pinned if waiting fails: KMS may have accepted the new buffer.
            while (!_pageFlipCompleted)
            {
                var pfd = new PollFd { events = 1, fd = _card.Fd };
                if (poll(&pfd, new IntPtr(1), -1) < 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 4) // EINTR
                        continue;
                    throw new Win32Exception(error, "Waiting for DRM page flip failed");
                }
                if ((pfd.revents & (8 | 16 | 32)) != 0) // POLLERR | POLLHUP | POLLNVAL
                    throw new InvalidOperationException("DRM page flip event source is unavailable");
                if (drmHandleEvent(_card.Fd, &context) != 0)
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error == 4) // EINTR
                        continue;
                    throw new Win32Exception(error, "Reading DRM page flip event failed");
                }
            }

            var previous = _scanoutBuffer;
            _scanoutBuffer = _pendingBuffer;
            _pendingBuffer = null;
            _pendingFlipHandle.Free();
            // Release through the buffer's original surface, after KMS has stopped scanning it.
            previous?.Dispose();
        }

        private bool UsePartialPresentation => _outputOptions.UsePartialPresentation == true;

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget()
        {
            if (_renderSwapchain.Item.IsCorrupted)
            {
                var replacement = RefCountable.Create(
                    new GbmEglSwapchain(_card, _gbmDevice, _eglDisplay, _mode.Resolution));
                var previous = _renderSwapchain;
                _renderSwapchain = replacement;
                // The old scanout lease keeps its surfaces alive until a replacement flip completes.
                previous.Dispose();
            }
            return new RenderTarget(this, _renderSwapchain.Clone());
        }


        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            if (context != _deferredContext)
                throw new InvalidOperationException(
                    "This platform backend can only create render targets for its primary context");
            return CreateGlRenderTarget();
        }

        private sealed class RenderTarget : EglPlatformSurfaceRenderTargetBase
        {
            private readonly DrmOutput _parent;
            private readonly IRef<GbmEglSwapchain> _swapchain;
            private readonly Action _pageFlip;
            private bool _disposed;

            public RenderTarget(DrmOutput parent, IRef<GbmEglSwapchain> swapchain) : base(parent._deferredContext)
            {
                _parent = parent;
                _swapchain = swapchain;
                _pageFlip = () => parent.PageFlip(_swapchain);
            }

            protected override RenderTargetProperties Properties => base.Properties with
            {
                RequiredRotationForOrientation = _parent.Orientation
            };

            protected override bool UsePartialPresentation => _parent.UsePartialPresentation;
            protected override bool SkipWaits => true;
            public override bool IsCorrupted => _disposed || base.IsCorrupted || _swapchain.Item.IsCorrupted;

            public override void Dispose()
            {
                if (_disposed)
                    return;
                _disposed = true;
                try
                {
                    if (base.IsCorrupted)
                        _swapchain.Item.IsCorrupted = true;
                    base.Dispose();
                }
                finally
                {
                    _swapchain.Dispose();
                }
            }

            public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
            {
                var swapchain = _swapchain.Item;
                // Retire completed flips without blocking; wait only if EGL needs a buffer released.
                if (_parent._pendingBuffer != null &&
                    (_parent._pageFlipCompleted || !swapchain.HasFreeBuffers))
                    _parent.WaitForPageFlip();
                var size = swapchain.Size;
                var session = base.BeginDraw(swapchain.EglSurface, size, _parent.Scaling, onFinish: _pageFlip);
                try
                {
                    Context.GlInterface.Viewport(0, 0, size.Width, size.Height);
                    return session;
                }
                catch
                {
                    session.Dispose();
                    throw;
                }
            }
        }

        public IGlContext CreateContext()
        {
            throw new NotImplementedException();
        }
    }


}
