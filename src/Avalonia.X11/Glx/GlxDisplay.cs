using System;
using System.Linq;
using Avalonia.OpenGL;
using static Avalonia.X11.Glx.GlxConsts;

namespace Avalonia.X11.Glx
{
    unsafe class GlxDisplay : IGlDisplay
    {
        private readonly X11Info _x11;
        private readonly IntPtr _fbconfig;
        private readonly XVisualInfo* _visual;
        public GlDisplayType Type => GlDisplayType.OpenGL2;
        public GlInterface GlInterface { get; }
        
        public XVisualInfo* VisualInfo => _visual;
        public int SampleCount { get; }
        public int StencilSize { get; }
        
        public GlxContext ImmediateContext { get; }
        public GlxContext DeferredContext { get; }
        public GlxInterface Glx { get; } = new GlxInterface();
        public GlxDisplay(X11Info x11) 
        {
            _x11 = x11;

            var baseAttribs = new[]
            {
                GLX_X_RENDERABLE, 1,
                GLX_RENDER_TYPE, GLX_RGBA_BIT,
                GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT | GLX_PBUFFER_BIT,
                GLX_DOUBLEBUFFER, 1,
                GLX_RED_SIZE, 8,
                GLX_GREEN_SIZE, 8,
                GLX_BLUE_SIZE, 8,
                GLX_ALPHA_SIZE, 8,
                GLX_DEPTH_SIZE, 1,
                GLX_STENCIL_SIZE, 8,

            };

            foreach (var attribs in new[]
            {
                //baseAttribs.Concat(multiattribs),
                baseAttribs,
            })
            {
                var ptr = Glx.ChooseFBConfig(_x11.Display, x11.DefaultScreen,
                    attribs, out var count);
                for (var c = 0 ; c < count; c++)
                {
                    
                    var visual = Glx.GetVisualFromFBConfig(_x11.Display, ptr[c]);
                    // We prefer 32 bit visuals
                    if (_fbconfig == IntPtr.Zero || visual->depth == 32)
                    {
                        _fbconfig = ptr[c];
                        _visual = visual;
                        if(visual->depth == 32)
                            break;
                    }
                }

                if (_fbconfig != IntPtr.Zero)
                    break;
            }

            if (_fbconfig == IntPtr.Zero)
                throw new OpenGlException("Unable to choose FBConfig");
            
            if (_visual == null)
                throw new OpenGlException("Unable to get visual info from FBConfig");
            if (Glx.GetFBConfigAttrib(_x11.Display, _fbconfig, GLX_SAMPLES, out var samples) == 0)
                SampleCount = samples;
            if (Glx.GetFBConfigAttrib(_x11.Display, _fbconfig, GLX_STENCIL_SIZE, out var stencil) == 0)
                StencilSize = stencil;

            var pbuffers = Enumerable.Range(0, 2).Select(_ => Glx.CreatePbuffer(_x11.Display, _fbconfig, new[]
            {
                GLX_PBUFFER_WIDTH, 1, GLX_PBUFFER_HEIGHT, 1, 0
            })).ToList();
            
            XLib.XFlush(_x11.Display);
            
            ImmediateContext = CreateContext(pbuffers[0],null);
            DeferredContext = CreateContext(pbuffers[1], ImmediateContext);
            ImmediateContext.MakeCurrent();
            var err = Glx.GetError();
            
            GlInterface = new GlInterface(GlxInterface.GlxGetProcAddress);
            if (GlInterface.Version == null)
                throw new OpenGlException("GL version string is null, aborting");
            if (GlInterface.Renderer == null)
                throw new OpenGlException("GL renderer string is null, aborting");

            if (Environment.GetEnvironmentVariable("AVALONIA_GLX_IGNORE_RENDERER_BLACKLIST") != "1")
            {
                var blacklist = AvaloniaLocator.Current.GetService<X11PlatformOptions>()
                    ?.GlxRendererBlacklist;
                if (blacklist != null)
                    foreach(var item in blacklist)
                        if (GlInterface.Renderer.Contains(item))
                            throw new OpenGlException($"Renderer '{GlInterface.Renderer}' is blacklisted by '{item}'");
            }
            
        }

        public void ClearContext() => Glx.MakeContextCurrent(_x11.Display,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        public GlxContext CreateContext(IGlContext share) => CreateContext(IntPtr.Zero, share);
        public GlxContext CreateContext(IntPtr defaultXid, IGlContext share)
        {
            var sharelist = ((GlxContext)share)?.Handle ?? IntPtr.Zero;
            IntPtr handle = default;
            foreach (var ver in new[]
            {
                new Version(4, 0), new Version(3, 2),
                new Version(3, 0), new Version(2, 0)
            })
            {

                var attrs = new[]
                {
                    GLX_CONTEXT_MAJOR_VERSION_ARB, ver.Major,
                    GLX_CONTEXT_MINOR_VERSION_ARB, ver.Minor,
                    GLX_CONTEXT_PROFILE_MASK_ARB, GLX_CONTEXT_CORE_PROFILE_BIT_ARB,
                    0
                };
                try
                {
                    handle = Glx.CreateContextAttribsARB(_x11.Display, _fbconfig, sharelist, true, attrs);
                    if (handle != IntPtr.Zero)
                        break;
                }
                catch
                {
                    break;
                }
            }
            
            if (handle == IntPtr.Zero)
                throw new OpenGlException("Unable to create direct GLX context");
            return new GlxContext(new GlxInterface(), handle, this, _x11, defaultXid);
        }

        public void SwapBuffers(IntPtr xid) => Glx.SwapBuffers(_x11.Display, xid);
    }
}
