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

        private GbmBoUserDataDestroyCallbackDelegate FbDestroyDelegate;
        private drmModeModeInfo _mode;
        private EglDisplay _eglDisplay;
        private EglSurface _eglSurface;
        private EglContext _deferredContext;
        private IntPtr _currentBo;
        private IntPtr _pendingBo;
        private volatile bool _flipPending;
        private GCHandle _flipUserData;
        private static readonly DrmEventPageFlipHandlerDelegate s_flipCallback = OnPageFlipComplete;
        private IntPtr _gbmTargetSurface;
        private uint _crtcId;

        void FbDestroyCallback(IntPtr bo, IntPtr userData)
        {
            drmModeRmFB(_card.Fd, userData.ToInt32());
        }

        uint GetFbIdForBo(IntPtr bo)
        {
            if (bo == IntPtr.Zero)
                throw new ArgumentException("bo is 0");
            var data = gbm_bo_get_user_data(bo);
            if (data != IntPtr.Zero)
                return (uint)data.ToInt32();

            var w = gbm_bo_get_width(bo);
            var h = gbm_bo_get_height(bo);
            var stride = gbm_bo_get_stride(bo);
            var handle = gbm_bo_get_handle(bo).u32;
            var format = gbm_bo_get_format(bo);

            // prepare for the new ioctl call
            var handles = new uint[] { handle, 0, 0, 0 };
            var pitches = new uint[] { stride, 0, 0, 0 };
            var offsets = new uint[4];

            var ret = drmModeAddFB2(_card.Fd, w, h, format, handles, pitches,
                                    offsets, out var fbHandle, 0);

            if (ret != 0)
            {
                // legacy fallback
                ret = drmModeAddFB(_card.Fd, w, h, 24, 32, stride, (uint)handle,
                                   out fbHandle);

                if (ret != 0)
                    throw new Win32Exception(ret, $"drmModeAddFb failed {ret}");
            }

            gbm_bo_set_user_data(bo, new IntPtr((int)fbHandle), FbDestroyDelegate);


            return fbHandle;
        }

        [MemberNotNull(nameof(_card))]
        [MemberNotNull(nameof(PlatformGraphics))]
        [MemberNotNull(nameof(FbDestroyDelegate))]
        [MemberNotNull(nameof(_eglDisplay))]
        [MemberNotNull(nameof(_eglSurface))]
        [MemberNotNull(nameof(_deferredContext))]
        void Init(DrmCard card, DrmResources resources, DrmConnector connector, DrmModeInfo modeInfo)
        {
            FbDestroyDelegate = FbDestroyCallback;
            _card = card;
            _flipUserData = GCHandle.Alloc(this, GCHandleType.Weak);
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
            var device = gbm_create_device(card.Fd);
            _gbmTargetSurface = gbm_surface_create(device, modeInfo.Resolution.Width, modeInfo.Resolution.Height,
                GbmColorFormats.GBM_FORMAT_XRGB8888, GbmBoFlags.GBM_BO_USE_SCANOUT | GbmBoFlags.GBM_BO_USE_RENDERING);
            if (_gbmTargetSurface == IntPtr.Zero)
                throw new InvalidOperationException("Unable to create GBM surface");

            _eglDisplay = new EglDisplay(
                new EglDisplayCreationOptions
                {
                    Egl = new EglInterface(eglGetProcAddress),
                    PlatformType = 0x31D7,
                    PlatformDisplay = device,
                    SupportsMultipleContexts = true,
                    SupportsContextSharing = true
                });

            var surface = _eglDisplay.EglInterface.CreateWindowSurface(_eglDisplay.Handle, _eglDisplay.Config, _gbmTargetSurface, new[] { EglConsts.EGL_NONE, EglConsts.EGL_NONE });

            _eglSurface = new EglSurface(_eglDisplay, surface);

            _deferredContext = _eglDisplay.CreateContext(null);
            PlatformGraphics = new SharedContextGraphics(_deferredContext);

            var initialBufferSwappingColorR = _outputOptions.InitialBufferSwappingColor.R / 255.0f;
            var initialBufferSwappingColorG = _outputOptions.InitialBufferSwappingColor.G / 255.0f;
            var initialBufferSwappingColorB = _outputOptions.InitialBufferSwappingColor.B / 255.0f;
            var initialBufferSwappingColorA = _outputOptions.InitialBufferSwappingColor.A / 255.0f;
            using (_deferredContext.MakeCurrent(_eglSurface))
            {
                _deferredContext.GlInterface.ClearColor(initialBufferSwappingColorR, initialBufferSwappingColorG,
                    initialBufferSwappingColorB, initialBufferSwappingColorA);
                _deferredContext.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_STENCIL_BUFFER_BIT);
                _eglSurface.SwapBuffers();
            }

            var bo = gbm_surface_lock_front_buffer(_gbmTargetSurface);
            var fbId = GetFbIdForBo(bo);
            var connectorId = connector.Id;
            var mode = modeInfo.Mode;


            var res = drmModeSetCrtc(_card.Fd, _crtcId, fbId, 0, 0, &connectorId, 1, &mode);
            if (res != 0)
                throw new Win32Exception(res, "drmModeSetCrtc failed");

            _mode = mode;
            _currentBo = bo;
            
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
                    }
            }

        }

        private void PageFlip()
        {
            var nextBo = gbm_surface_lock_front_buffer(_gbmTargetSurface);
            if (nextBo == IntPtr.Zero)
            {
                // Not sure what else can be done
                Console.WriteLine("gbm_surface_lock_front_buffer failed");
            }
            else
            {

                var fb = GetFbIdForBo(nextBo);

                // Only wait for the previously queued flip, so the next frame can be rendered
                // while this one waits for vblank.
                WaitForPendingFlip();

                // Outputs can share the card's fd, so tag the flip to get its completion back to this output
                _flipPending = true;
                if (drmModePageFlip(_card.Fd, _crtcId, fb, DrmModePageFlip.Event,
                        (void*)GCHandle.ToIntPtr(_flipUserData)) == 0)
                {
                    _pendingBo = nextBo;

                    // No buffer left to render the next frame into, wait for the flip right away
                    if (gbm_surface_has_free_buffers(_gbmTargetSurface) == 0)
                        WaitForPendingFlip();
                }
                else
                {
                    _flipPending = false;
                    gbm_surface_release_buffer(_gbmTargetSurface, nextBo);
                }
            }
        }

        private void WaitForPendingFlip()
        {
            if (_pendingBo == IntPtr.Zero)
                return;

            var ctx = new DrmEventContext
            {
                version = 4,
                page_flip_handler = Marshal.GetFunctionPointerForDelegate(s_flipCallback)
            };
            while (_flipPending)
            {
                var pfd = new PollFd {events = 1, fd = _card.Fd};
                poll(&pfd, new IntPtr(1), -1);
                drmHandleEvent(_card.Fd, &ctx);
            }

            gbm_surface_release_buffer(_gbmTargetSurface, _currentBo);
            _currentBo = _pendingBo;
            _pendingBo = IntPtr.Zero;
        }

        private static void OnPageFlipComplete(int fd, uint sequence, uint tv_sec, uint tv_usec, void* user_data)
        {
            if (user_data != null && GCHandle.FromIntPtr(new IntPtr(user_data)).Target is DrmOutput output)
                output._flipPending = false;
        }

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget() => new RenderTarget(this);

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
            private readonly EglSurface _surface;
            private readonly Action _pageFlip;

            public RenderTarget(DrmOutput parent) : base(parent._deferredContext)
            {
                _parent = parent;
                _surface = parent._eglSurface;
                _pageFlip = parent.PageFlip;
            }

            protected override RenderTargetProperties Properties => base.Properties with
            {
                RequiredRotationForOrientation = _parent.Orientation
            };

            protected override bool SkipWaits => true;

            public override IGlPlatformSurfaceRenderingSession BeginDrawCore(IRenderTarget.RenderTargetSceneInfo sceneInfo)
            {
                var size = _parent._mode.Resolution;
                var session = base.BeginDraw(_surface, size, _parent.Scaling, onFinish: _pageFlip);
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
