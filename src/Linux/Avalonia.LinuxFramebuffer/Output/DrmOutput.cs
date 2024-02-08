using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Egl;
using Avalonia.OpenGL.Surfaces;
using Avalonia.Platform;
using Avalonia.Platform.Interop;
using static Avalonia.LinuxFramebuffer.NativeUnsafeMethods;
using static Avalonia.LinuxFramebuffer.Output.LibDrm;
using static Avalonia.LinuxFramebuffer.Output.LibDrm.GbmColorFormats;

namespace Avalonia.LinuxFramebuffer.Output
{
    public unsafe class DrmOutput : IGlOutputBackend, IGlPlatformSurface
    {
        private DrmOutputOptions _outputOptions = new();
        private DrmCard _card;
        public PixelSize PixelSize => _mode.Resolution;

        public double Scaling
        {
            get => _outputOptions.Scaling;
            set => _outputOptions.Scaling = value;
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
            DrmOutputOptions options = null)
        {
            if(options != null) 
                _outputOptions = options;
            Init(card, resources, connector, modeInfo);
        }
        
        public DrmOutput(string path = null, bool connectorsForceProbe = false, DrmOutputOptions options = null)
        {
            if(options != null) 
                _outputOptions = options;
            
            var card = new DrmCard(path);

            var resources = card.GetResources(connectorsForceProbe);

            var connector =
                resources.Connectors.FirstOrDefault(x => x.Connection == DrmModeConnection.DRM_MODE_CONNECTED);
            if(connector == null)
                throw new InvalidOperationException("Unable to find connected DRM connector");

            DrmModeInfo mode = null;

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
            if(mode == null)
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
            var handles = new uint[] {handle, 0, 0, 0};
            var pitches = new uint[] {stride, 0, 0, 0};
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
        
        
        void Init(DrmCard card, DrmResources resources, DrmConnector connector, DrmModeInfo modeInfo)
        {
            FbDestroyDelegate = FbDestroyCallback;
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
                            && encoder.PossibleCrtcs.Count>0)
                            return encoder.PossibleCrtcs.First().crtc_id;
                    }

                    throw new InvalidOperationException("Unable to find CRTC matching the desired mode");
                }
            }

            _crtcId = GetCrtc();
            var device = gbm_create_device(card.Fd);
            _gbmTargetSurface = gbm_surface_create(device, modeInfo.Resolution.Width, modeInfo.Resolution.Height,
                GbmColorFormats.GBM_FORMAT_XRGB8888, GbmBoFlags.GBM_BO_USE_SCANOUT | GbmBoFlags.GBM_BO_USE_RENDERING);
            if(_gbmTargetSurface == IntPtr.Zero)
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
                //Go trough two cycles of buffer swapping (there are render artifacts otherwise)
                for(var c=0;c<2;c++)
                    using (CreateGlRenderTarget().BeginDraw())
                    {
                        _deferredContext.GlInterface.ClearColor(initialBufferSwappingColorR, initialBufferSwappingColorG, 
                            initialBufferSwappingColorB, initialBufferSwappingColorA);
                        _deferredContext.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT | GlConsts.GL_STENCIL_BUFFER_BIT);
                    }
            }
            
        }

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget() => new RenderTarget(this);
        

        public IGlPlatformSurfaceRenderTarget CreateGlRenderTarget(IGlContext context)
        {
            if (context != _deferredContext)
                throw new InvalidOperationException(
                    "This platform backend can only create render targets for its primary context");
            return CreateGlRenderTarget();
        }
    
        class RenderTarget : IGlPlatformSurfaceRenderTarget
        {
            private readonly DrmOutput _parent;

            public RenderTarget(DrmOutput parent)
            {
                _parent = parent;
            }
            public void Dispose()
            {
                // We are wrapping GBM buffer chain associated with CRTC, and don't free it on a whim
            }

            class RenderSession : IGlPlatformSurfaceRenderingSession
            {
                private readonly DrmOutput _parent;
                private readonly IDisposable _clearContext;

                public RenderSession(DrmOutput parent, IDisposable clearContext)
                {
                    _parent = parent;
                    _clearContext = clearContext;
                }

                public void Dispose()
                {
                    _parent._deferredContext.GlInterface.Flush();
                    _parent._eglSurface.SwapBuffers();
                    
                    var nextBo = gbm_surface_lock_front_buffer(_parent._gbmTargetSurface);
                    if (nextBo == IntPtr.Zero)
                    {
                        // Not sure what else can be done
                        Console.WriteLine("gbm_surface_lock_front_buffer failed");
                    }
                    else
                    {

                        var fb = _parent.GetFbIdForBo(nextBo);
                        bool waitingForFlip = true;

                        drmModePageFlip(_parent._card.Fd, _parent._crtcId, fb, DrmModePageFlip.Event, null);

                        DrmEventPageFlipHandlerDelegate flipCb =
                            (int fd, uint sequence, uint tv_sec, uint tv_usec, void* user_data) =>
                            {
                                waitingForFlip = false;
                            };
                        var cbHandle = GCHandle.Alloc(flipCb);
                        var ctx = new DrmEventContext
                        {
                            version = 4, page_flip_handler = Marshal.GetFunctionPointerForDelegate(flipCb)
                        };
                        while (waitingForFlip)
                        {
                            var pfd = new pollfd {events = 1, fd = _parent._card.Fd};
                            poll(&pfd, new IntPtr(1), -1);
                            drmHandleEvent(_parent._card.Fd, &ctx);
                        }

                        cbHandle.Free();
                        gbm_surface_release_buffer(_parent._gbmTargetSurface, _parent._currentBo);
                        _parent._currentBo = nextBo;
                    }
                    _clearContext.Dispose();
                }


                public IGlContext Context => _parent._deferredContext;

                public PixelSize Size => _parent._mode.Resolution;

                public double Scaling => _parent.Scaling;

                public bool IsYFlipped { get; }
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                return new RenderSession(_parent, _parent._deferredContext.MakeCurrent(_parent._eglSurface));
            }
        }

        public IGlContext CreateContext()
        {
            throw new NotImplementedException();
        }
    }


}
