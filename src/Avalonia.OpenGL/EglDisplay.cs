using System;
using System.ComponentModel;
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

        public IntPtr Handle => _display;
        public EglDisplay(EglInterface egl)
        {
            _egl = egl;  

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _egl.GetPlatformDisplayEXT != null)
            {
                foreach (var dapi in new[] {EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE})
                {
                    _display = _egl.GetPlatformDisplayEXT(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero, new[]
                    {
                        EGL_PLATFORM_ANGLE_TYPE_ANGLE, dapi, EGL_NONE
                    });
                    if(_display != IntPtr.Zero)
                        break;
                }
            }

            if (_display == IntPtr.Zero)
                _display = _egl.GetDisplay(IntPtr.Zero);
            
            if(_display == IntPtr.Zero)
                throw new OpenGlException("eglGetDisplay failed");
            
            if (!_egl.Initialize(_display, out var major, out var minor))
                throw new OpenGlException("eglInitialize failed");

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

                var attribs = new[]
                {
                    EGL_SURFACE_TYPE, EGL_PBUFFER_BIT,
                    EGL_RENDERABLE_TYPE, cfg.RenderableTypeBit,
                    EGL_RED_SIZE, 8,
                    EGL_GREEN_SIZE, 8,
                    EGL_BLUE_SIZE, 8,
                    EGL_ALPHA_SIZE, 8,
                    EGL_STENCIL_SIZE, 8,
                    EGL_DEPTH_SIZE, 8,
                    EGL_NONE
                };
                if (!_egl.ChooseConfig(_display, attribs, out _config, 1, out int numConfigs))
                    continue;
                if (numConfigs == 0)
                    continue;
                _contextAttributes = cfg.Attributes;
                Type = cfg.Type;
            }

            if (_contextAttributes == null)
                throw new OpenGlException("No suitable EGL config was found");
            
            GlInterface = new GlInterface((proc, optional) =>
            {

                using (var u = new Utf8Buffer(proc))
                {
                    var rv = _egl.GetProcAddress(u);
                    if (rv == IntPtr.Zero && !optional)
                        throw new OpenGlException("Missing function " + proc);
                    return rv;
                }
            });
        }

        public EglDisplay() : this(new EglInterface())
        {
            
        }
        
        public GlDisplayType Type { get; }
        public GlInterface GlInterface { get; }
        public IGlContext CreateContext(IGlContext share)
        {
            var shareCtx = (EglContext)share;
            var ctx = _egl.CreateContext(_display, _config, shareCtx?.Context ?? IntPtr.Zero, _contextAttributes);
            if (ctx == IntPtr.Zero)
                throw new OpenGlException("eglCreateContext failed");
            var surf = _egl.CreatePBufferSurface(_display, _config, new[]
            {
                EGL_WIDTH, 1,
                EGL_HEIGHT, 1,
                EGL_NONE
            });
            if (surf == IntPtr.Zero)
                throw new OpenGlException("eglCreatePbufferSurface failed");
            var rv = new EglContext(this, _egl, ctx, surf);
            rv.MakeCurrent(null);
            return rv;
        }

        public void ClearContext()
        {
            if (!_egl.MakeCurrent(_display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
                throw new OpenGlException("eglMakeCurrent failed");
        }

        public EglSurface CreateWindowSurface(IntPtr window)
        {
            var s = _egl.CreateWindowSurface(_display, _config, window, new[] {EGL_NONE, EGL_NONE});
            if (s == IntPtr.Zero)
                throw new OpenGlException("eglCreateWindowSurface failed");
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
