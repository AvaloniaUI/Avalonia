using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;
using static Avalonia.OpenGL.EglConsts;

namespace Avalonia.OpenGL
{
    public class EglDisplay : IGlDisplay
    {
        private readonly EglInterface _egl;
        private readonly IntPtr _display;
        private readonly IntPtr _config;
        private readonly int[] _contextAttributes;
        private readonly int _surfaceType;

        public IntPtr Handle => _display;
        private AngleOptions.PlatformApi? _angleApi;

        public EglDisplay(EglInterface egl) : this(egl, -1, IntPtr.Zero, null)
        {
            
        }
        public EglDisplay(EglInterface egl, int platformType, IntPtr platformDisplay, int[] attrs)
        {
            _egl = egl;

            if (platformType == -1 && platformDisplay == IntPtr.Zero)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (_egl.GetPlatformDisplayEXT == null)
                        throw new OpenGlException("eglGetPlatformDisplayEXT is not supported by libegl.dll");

                    var allowedApis = AvaloniaLocator.Current.GetService<AngleOptions>()?.AllowedPlatformApis
                                      ?? new List<AngleOptions.PlatformApi> {AngleOptions.PlatformApi.DirectX9};

                    foreach (var platformApi in allowedApis)
                    {
                        int dapi;
                        if (platformApi == AngleOptions.PlatformApi.DirectX9)
                            dapi = EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE;
                        else if (platformApi == AngleOptions.PlatformApi.DirectX11)
                            dapi = EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE;
                        else
                            continue;

                        _display = _egl.GetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                            new[] {EGL_PLATFORM_ANGLE_TYPE_ANGLE, dapi, EGL_NONE});
                        if (_display != IntPtr.Zero)
                        {
                            _angleApi = platformApi;
                            break;
                        }
                    }

                    if (_display == IntPtr.Zero)
                        throw new OpenGlException("Unable to create ANGLE display");
                }

                if (_display == IntPtr.Zero)
                    _display = _egl.GetDisplay(IntPtr.Zero);
            }
            else
            {
                if (_egl.GetPlatformDisplayEXT == null)
                    throw new OpenGlException("eglGetPlatformDisplayEXT is not supported by libegl");
                _display = _egl.GetPlatformDisplayEXT(platformType, platformDisplay, attrs);
            }

            if (_display == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglGetDisplay", _egl);

            if (!_egl.Initialize(_display, out var major, out var minor))
                throw OpenGlException.GetFormattedException("eglInitialize", _egl);

            foreach (var cfg in new[]
            {
                new
                {
                    Attributes = new[] {EGL_NONE},
                    Api = EGL_OPENGL_API,
                    RenderableTypeBit = EGL_OPENGL_BIT,
                    Type = GlDisplayType.OpenGL2
                },
                new
                {
                    Attributes = new[]
                    {
                        EGL_CONTEXT_CLIENT_VERSION, 2,
                        EGL_NONE
                    },
                    Api = EGL_OPENGL_ES_API,
                    RenderableTypeBit = EGL_OPENGL_ES2_BIT,
                    Type = GlDisplayType.OpenGLES2
                }
            })
            {
                if (!_egl.BindApi(cfg.Api))
                    continue;
                foreach(var surfaceType in new[]{EGL_PBUFFER_BIT|EGL_WINDOW_BIT, EGL_WINDOW_BIT})
                foreach(var stencilSize in new[]{8, 1, 0})
                foreach (var depthSize in new []{8, 1, 0})
                {
                    var attribs = new[]
                    {
                        EGL_SURFACE_TYPE, surfaceType,
                        EGL_RENDERABLE_TYPE, cfg.RenderableTypeBit,
                        EGL_RED_SIZE, 8,
                        EGL_GREEN_SIZE, 8,
                        EGL_BLUE_SIZE, 8,
                        EGL_ALPHA_SIZE, 8,
                        EGL_STENCIL_SIZE, stencilSize,
                        EGL_DEPTH_SIZE, depthSize,
                        EGL_NONE
                    };
                    if (!_egl.ChooseConfig(_display, attribs, out _config, 1, out int numConfigs))
                        continue;
                    if (numConfigs == 0)
                        continue;
                    _contextAttributes = cfg.Attributes;
                    _surfaceType = surfaceType;
                    Type = cfg.Type;
                }
            }

            if (_contextAttributes == null)
                throw new OpenGlException("No suitable EGL config was found");
               
            GlInterface = GlInterface.FromNativeUtf8GetProcAddress(b => _egl.GetProcAddress(b));
        }

        public EglDisplay() : this(new EglInterface())
        {
            
        }
        
        public GlDisplayType Type { get; }
        public GlInterface GlInterface { get; }
        public EglInterface EglInterface => _egl;
        public EglContext CreateContext(IGlContext share)
        {
            if((_surfaceType|EGL_PBUFFER_BIT) == 0)
                throw new InvalidOperationException("Platform doesn't support PBUFFER surfaces");
            var shareCtx = (EglContext)share;
            var ctx = _egl.CreateContext(_display, _config, shareCtx?.Context ?? IntPtr.Zero, _contextAttributes);
            if (ctx == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglCreateContext", _egl);
            var surf = _egl.CreatePBufferSurface(_display, _config, new[]
            {
                EGL_WIDTH, 1,
                EGL_HEIGHT, 1,
                EGL_NONE
            });
            if (surf == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglCreatePBufferSurface", _egl);
            var rv = new EglContext(this, _egl, ctx, new EglSurface(this, _egl, surf));
            rv.MakeCurrent(null);
            return rv;
        }

        public EglContext CreateContext(EglContext share, EglSurface offscreenSurface)
        {
            var ctx = _egl.CreateContext(_display, _config, share?.Context ?? IntPtr.Zero, _contextAttributes);
            if (ctx == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglCreateContext", _egl);
            var rv = new EglContext(this, _egl, ctx, offscreenSurface);
            rv.MakeCurrent(null);
            return rv;
        }

        public void ClearContext()
        {
            if (!_egl.MakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
                throw OpenGlException.GetFormattedException("eglMakeCurrent", _egl);
        }

        public EglSurface CreateWindowSurface(IntPtr window)
        {
            var s = _egl.CreateWindowSurface(_display, _config, window, new[] {EGL_NONE, EGL_NONE});
            if (s == IntPtr.Zero)
                throw OpenGlException.GetFormattedException("eglCreateWindowSurface", _egl);
            return new EglSurface(this, _egl, s);
        }

        public int SampleCount
        {
            get
            {
                _egl.GetConfigAttrib(_display, _config, EGL_SAMPLES, out var rv);
                return rv;
            }
        }

        public int StencilSize
        {
            get
            {
                _egl.GetConfigAttrib(_display, _config, EGL_STENCIL_SIZE, out var rv);
                return rv;
            }
        }
    }
}
