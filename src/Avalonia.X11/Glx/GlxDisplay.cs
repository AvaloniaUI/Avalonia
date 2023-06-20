using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.OpenGL;
using static Avalonia.X11.Glx.GlxConsts;

namespace Avalonia.X11.Glx
{
    internal unsafe class GlxDisplay
    {
        private readonly X11Info _x11;
        private readonly GlVersion[] _probeProfiles;
        private readonly IntPtr _fbconfig;
        private readonly XVisualInfo* _visual;
        private string[] _displayExtensions;
        private GlVersion? _version;
        
        public XVisualInfo* VisualInfo => _visual;
        public GlxContext DeferredContext { get; }
        public GlxInterface Glx { get; } = new GlxInterface();
        public GlxDisplay(X11Info x11, IList<GlVersion> probeProfiles) 
        {
            _x11 = x11;
            _probeProfiles = probeProfiles.ToArray();
            _displayExtensions = Glx.GetExtensions(_x11.DeferredDisplay);

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
            int sampleCount = 0;
            int stencilSize = 0;
            foreach (var attribs in new[]
            {
                //baseAttribs.Concat(multiattribs),
                baseAttribs,
            })
            {
                var ptr = Glx.ChooseFBConfig(_x11.DeferredDisplay, x11.DefaultScreen,
                    attribs, out var count);
                for (var c = 0 ; c < count; c++)
                {
                    
                    var visual = Glx.GetVisualFromFBConfig(_x11.DeferredDisplay, ptr[c]);
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
            if (Glx.GetFBConfigAttrib(_x11.DeferredDisplay, _fbconfig, GLX_SAMPLES, out var samples) == 0)
                sampleCount = samples;
            if (Glx.GetFBConfigAttrib(_x11.DeferredDisplay, _fbconfig, GLX_STENCIL_SIZE, out var stencil) == 0)
                stencilSize = stencil;

            var attributes = new[] { GLX_PBUFFER_WIDTH, 1, GLX_PBUFFER_HEIGHT, 1, 0 };
            
            Glx.CreatePbuffer(_x11.DeferredDisplay, _fbconfig, attributes);
            Glx.CreatePbuffer(_x11.DeferredDisplay, _fbconfig, attributes);
            
            XLib.XFlush(_x11.DeferredDisplay);

            DeferredContext = CreateContext(CreatePBuffer(), null,
                sampleCount, stencilSize, true);
            using (DeferredContext.MakeCurrent())
            {
                var glInterface = DeferredContext.GlInterface;
                if (glInterface.Version == null)
                    throw new OpenGlException("GL version string is null, aborting");
                if (glInterface.Renderer == null)
                    throw new OpenGlException("GL renderer string is null, aborting");

                if (Environment.GetEnvironmentVariable("AVALONIA_GLX_IGNORE_RENDERER_BLACKLIST") != "1")
                {
                    var opts = AvaloniaLocator.Current.GetService<X11PlatformOptions>() ?? new X11PlatformOptions();
                    var blacklist = opts.GlxRendererBlacklist;
                    if (blacklist != null)
                        foreach (var item in blacklist)
                            if (glInterface.Renderer.Contains(item))
                                throw new OpenGlException(
                                    $"Renderer '{glInterface.Renderer}' is blacklisted by '{item}'");
                }
            }
        }

        private IntPtr CreatePBuffer()
        {
            return Glx.CreatePbuffer(_x11.DeferredDisplay, _fbconfig, new[] { GLX_PBUFFER_WIDTH, 1, GLX_PBUFFER_HEIGHT, 1, 0 });
        }

        public GlxContext CreateContext() => CreateContext(CreatePBuffer(), null, DeferredContext.SampleCount,
            DeferredContext.StencilSize, true);
        
        public GlxContext CreateContext(IGlContext share) => CreateContext(CreatePBuffer(), share,
            share.SampleCount, share.StencilSize, true);

        private GlxContext CreateContext(IntPtr defaultXid, IGlContext share,
            int sampleCount, int stencilSize, bool ownsPBuffer)
        {
            var sharelist = ((GlxContext)share)?.Handle ?? IntPtr.Zero;
            IntPtr handle = default;
            
            GlxContext Create(GlVersion profile)
            {
                var profileMask = GLX_CONTEXT_CORE_PROFILE_BIT_ARB;
                if (profile.Type == GlProfileType.OpenGLES) 
                    profileMask = GLX_CONTEXT_ES2_PROFILE_BIT_EXT;

                var attrs = new int[]
                {
                    GLX_CONTEXT_MAJOR_VERSION_ARB, profile.Major,
                    GLX_CONTEXT_MINOR_VERSION_ARB, profile.Minor,
                    GLX_CONTEXT_PROFILE_MASK_ARB, profileMask,
                    0
                };
                
                try
                {
                    handle = Glx.CreateContextAttribsARB(_x11.DeferredDisplay, _fbconfig, sharelist, true, attrs);
                    if (handle != IntPtr.Zero)
                    {
                        _version = profile;
                        return new GlxContext(new GlxInterface(), handle, this, (GlxContext)share, profile,
                            sampleCount, stencilSize, _x11, defaultXid, ownsPBuffer);
                        
                    }
                }
                catch
                {
                    return null;
                }

                return null;
            }

            GlxContext rv = null;
            if (_version.HasValue)
            {
                rv = Create(_version.Value);
            }
            
            foreach (var v in _probeProfiles)
            {
                if (v.Type == GlProfileType.OpenGLES
                    && !_displayExtensions.Contains("GLX_EXT_create_context_es2_profile"))
                    continue;
                rv = Create(v);
                if (rv != null)
                {
                    _version = v;
                    break;
                }
            }

            if (rv != null)
                return rv;

            throw new OpenGlException("Unable to create direct GLX context");
        }

        public void SwapBuffers(IntPtr xid) => Glx.SwapBuffers(_x11.DeferredDisplay, xid);
    }
}
