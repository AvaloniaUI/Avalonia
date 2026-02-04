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
        private IntPtr _gbmTargetSurface;
        private uint _crtcId;
        private int _rotationFbo;
        private int _rotationTexture;
        private PixelSize _rotatedSize;
        private int _rotationProgram;
        private int _rotationVbo;
        private int _rotationVao;

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
            
            // Initialize FBO for rotation if needed
            var needsRotation = _outputOptions.Orientation != SurfaceOrientation.Rotation0;
            if (needsRotation)
            {
                // For 90/270 rotation, swap width and height
                _rotatedSize = (_outputOptions.Orientation == SurfaceOrientation.Rotation90 || 
                               _outputOptions.Orientation == SurfaceOrientation.Rotation270)
                    ? new PixelSize(modeInfo.Resolution.Height, modeInfo.Resolution.Width)
                    : modeInfo.Resolution;
                    
                using (_deferredContext.MakeCurrent(_eglSurface))
                {
                    var gl = _deferredContext.GlInterface;
                    _rotationFbo = gl.GenFramebuffer();
                    _rotationTexture = gl.GenTexture();
                    
                    gl.BindTexture(GlConsts.GL_TEXTURE_2D, _rotationTexture);
                    gl.TexImage2D(GlConsts.GL_TEXTURE_2D, 0, GlConsts.GL_RGBA, _rotatedSize.Width, _rotatedSize.Height, 0,
                        GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_BYTE, IntPtr.Zero);
                    gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MIN_FILTER, GlConsts.GL_LINEAR);
                    gl.TexParameteri(GlConsts.GL_TEXTURE_2D, GlConsts.GL_TEXTURE_MAG_FILTER, GlConsts.GL_LINEAR);
                    
                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _rotationFbo);
                    gl.FramebufferTexture2D(GlConsts.GL_FRAMEBUFFER, GlConsts.GL_COLOR_ATTACHMENT0,
                        GlConsts.GL_TEXTURE_2D, _rotationTexture, 0);
                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                    
                    // Create shader program for textured quad
                    const string vertexShader = @"
                        attribute vec2 aPos;
                        attribute vec2 aTexCoord;
                        varying vec2 vTexCoord;
                        void main() {
                            gl_Position = vec4(aPos, 0.0, 1.0);
                            vTexCoord = aTexCoord;
                        }";
                    
                    const string fragmentShader = @"
                        precision mediump float;
                        varying vec2 vTexCoord;
                        uniform sampler2D uTexture;
                        void main() {
                            gl_FragColor = texture2D(uTexture, vTexCoord);
                        }";
                    
                    var vs = gl.CreateShader(GlConsts.GL_VERTEX_SHADER);
                    gl.ShaderSourceString(vs, vertexShader);
                    gl.CompileShader(vs);
                    
                    var fs = gl.CreateShader(GlConsts.GL_FRAGMENT_SHADER);
                    gl.ShaderSourceString(fs, fragmentShader);
                    gl.CompileShader(fs);
                    
                    _rotationProgram = gl.CreateProgram();
                    gl.AttachShader(_rotationProgram, vs);
                    gl.AttachShader(_rotationProgram, fs);
                    gl.LinkProgram(_rotationProgram);
                    gl.DeleteShader(vs);
                    gl.DeleteShader(fs);
                    
                    // Create VBO with quad vertices - texture coords depend on rotation
                    // Format: x, y, u, v
                    float[] vertices = _outputOptions.Orientation switch
                    {
                        SurfaceOrientation.Rotation90 => new float[] {
                            // 90° clockwise rotation
                            -1.0f, -1.0f,        1.0f, 0.0f,  // Bottom-left -> Bottom-right of texture
                             1.0f, -1.0f,        1.0f, 1.0f,  // Bottom-right -> Top-right of texture
                             1.0f,  1.0f,        0.0f, 1.0f,  // Top-right -> Top-left of texture
                            -1.0f,  1.0f,        0.0f, 0.0f   // Top-left -> Bottom-left of texture
                        },
                        SurfaceOrientation.Rotation180 => new float[] {
                            // 180° rotation
                            -1.0f, -1.0f,        1.0f, 1.0f,  // Bottom-left -> Top-right of texture
                             1.0f, -1.0f,        0.0f, 1.0f,  // Bottom-right -> Top-left of texture
                             1.0f,  1.0f,        0.0f, 0.0f,  // Top-right -> Bottom-left of texture
                            -1.0f,  1.0f,        1.0f, 0.0f   // Top-left -> Bottom-right of texture
                        },
                        SurfaceOrientation.Rotation270 => new float[] {
                            // 270° clockwise (90° counter-clockwise) rotation
                            -1.0f, -1.0f,        0.0f, 1.0f,  // Bottom-left -> Top-left of texture
                             1.0f, -1.0f,        0.0f, 0.0f,  // Bottom-right -> Bottom-left of texture
                             1.0f,  1.0f,        1.0f, 0.0f,  // Top-right -> Bottom-right of texture
                            -1.0f,  1.0f,        1.0f, 1.0f   // Top-left -> Top-right of texture
                        },
                        _ => new float[] {
                            // No rotation (shouldn't reach here but fallback)
                            -1.0f, -1.0f,        0.0f, 0.0f,
                             1.0f, -1.0f,        1.0f, 0.0f,
                             1.0f,  1.0f,        1.0f, 1.0f,
                            -1.0f,  1.0f,        0.0f, 1.0f
                        }
                    };

                    _rotationVbo = gl.GenBuffer();
                    _rotationVao = gl.GenVertexArray();
                    
                    gl.BindVertexArray(_rotationVao);
                    gl.BindBuffer(GlConsts.GL_ARRAY_BUFFER, _rotationVbo);
                    
                    fixed (float* ptr = vertices)
                    {
                        gl.BufferData(GlConsts.GL_ARRAY_BUFFER, new IntPtr(vertices.Length * sizeof(float)), 
                            new IntPtr(ptr), GlConsts.GL_STATIC_DRAW);
                    }
                    
                    var posAttrib = gl.GetAttribLocationString(_rotationProgram, "aPos");
                    gl.EnableVertexAttribArray(posAttrib);
                    gl.VertexAttribPointer(posAttrib, 2, GlConsts.GL_FLOAT, 0, 4 * sizeof(float), IntPtr.Zero);
                    
                    var texAttrib = gl.GetAttribLocationString(_rotationProgram, "aTexCoord");
                    gl.EnableVertexAttribArray(texAttrib);
                    gl.VertexAttribPointer(texAttrib, 2, GlConsts.GL_FLOAT, 0, 4 * sizeof(float), new IntPtr(2 * sizeof(float)));
                    
                    gl.BindVertexArray(0);
                }
            }
            else
            {
                // No rotation needed
                _rotatedSize = modeInfo.Resolution;
            }

            if (_outputOptions.EnableInitialBufferSwapping)
            {
                //Go through two cycles of buffer swapping (there are render artifacts otherwise)
                for (var c = 0; c < 2; c++)
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
                    var gl = _parent._deferredContext.GlInterface;
                    
                    if (_parent._outputOptions.Orientation != SurfaceOrientation.Rotation0)
                    {
                        // Rotation enabled - blit from FBO to screen
                        // Unbind FBO to render to default framebuffer
                        gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                        gl.Viewport(0, 0, _parent._mode.Resolution.Width, _parent._mode.Resolution.Height);
                        
                        // Clear the screen
                        gl.ClearColor(0, 0, 0, 1);
                        gl.Clear(GlConsts.GL_COLOR_BUFFER_BIT);
                        
                        // Use the shader program
                        gl.UseProgram(_parent._rotationProgram);
                        
                        // Bind the FBO texture
                        gl.ActiveTexture(GlConsts.GL_TEXTURE0);
                        gl.BindTexture(GlConsts.GL_TEXTURE_2D, _parent._rotationTexture);
                        
                        // Set texture uniform (texture unit 0)
                        var texLoc = gl.GetUniformLocationString(_parent._rotationProgram, "uTexture");
                        gl.Uniform1i(texLoc, 0);
                        
                        // Draw the rotated quad
                        gl.BindVertexArray(_parent._rotationVao);
                        gl.DrawArrays(GlConsts.GL_TRIANGLE_FAN, 0, 4);
                        gl.BindVertexArray(0);
                        
                        gl.UseProgram(0);
                    }
                    
                    gl.Flush();
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
                            version = 4,
                            page_flip_handler = Marshal.GetFunctionPointerForDelegate(flipCb)
                        };
                        while (waitingForFlip)
                        {
                            var pfd = new PollFd {events = 1, fd = _parent._card.Fd};
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

                public PixelSize Size => _parent._rotatedSize;

                public double Scaling => _parent.Scaling;

                public bool IsYFlipped => false;
            }

            public IGlPlatformSurfaceRenderingSession BeginDraw()
            {
                var clearContext = _parent._deferredContext.MakeCurrent(_parent._eglSurface);
                var gl = _parent._deferredContext.GlInterface;
                
                if (_parent._outputOptions.Orientation != SurfaceOrientation.Rotation0)
                {
                    // Bind FBO for rendering when rotation is enabled
                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, _parent._rotationFbo);
                    gl.Viewport(0, 0, _parent._rotatedSize.Width, _parent._rotatedSize.Height);
                }
                else
                {
                    // Render directly to screen when no rotation
                    gl.BindFramebuffer(GlConsts.GL_FRAMEBUFFER, 0);
                    gl.Viewport(0, 0, _parent._mode.Resolution.Width, _parent._mode.Resolution.Height);
                }
                
                return new RenderSession(_parent, clearContext);
            }
        }

        public IGlContext CreateContext()
        {
            throw new NotImplementedException();
        }
    }


}
