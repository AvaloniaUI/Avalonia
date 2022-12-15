using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using Avalonia.Platform;
using OpenGLES;

namespace Avalonia.iOS
{
    class EaglPlatformGraphics : IPlatformGraphics
    {
        public IPlatformGraphicsContext GetSharedContext() => Context;

        public bool UsesSharedContext => true;
        public IPlatformGraphicsContext CreateContext() => throw new System.NotSupportedException();
        public GlContext Context { get; }
        public static GlVersion GlVersion { get; } = new(GlProfileType.OpenGLES, 3, 0);

        public EaglPlatformGraphics()
        {
            
            const string path = "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";
            var libGl = ObjCRuntime.Dlfcn.dlopen(path, 1);
            if (libGl == IntPtr.Zero)
                throw new OpenGlException("Unable to load " + path);
            var iface = new GlInterface(GlVersion, proc => ObjCRuntime.Dlfcn.dlsym(libGl, proc));
            Context = new(iface, null);
        }
    }

    class GlContext : IGlContext
    {
        public EAGLContext Context { get; private set; }
        
        public GlContext(GlInterface glInterface, EAGLSharegroup sharegroup)
        {
            GlInterface = glInterface;
            Context = sharegroup == null ?
                new EAGLContext(EAGLRenderingAPI.OpenGLES3) :
                new(EAGLRenderingAPI.OpenGLES3, sharegroup);
        }
        
        public void Dispose()
        {
            Context?.Dispose();
            Context = null;
        }

        class ResetContext : IDisposable
        {
            private EAGLContext _old;
            private bool _disposed;

            public ResetContext(EAGLContext old)
            {
                _old = old;
            }
            
            public void Dispose()
            {
                if(_disposed)
                    return;
                _disposed = true;
                EAGLContext.SetCurrentContext(_old);
                _old = null;
            }
        }
        
        public IDisposable MakeCurrent()
        {
            if (Context == null)
                throw new PlatformGraphicsContextLostException();
            var old = EAGLContext.CurrentContext;
            if (!EAGLContext.SetCurrentContext(Context))
                throw new OpenGlException("Unable to make context current");
            return new ResetContext(old);
        }

        public bool IsLost => Context == null;

        public IDisposable EnsureCurrent()
        {
            if (Context == null)
                throw new PlatformGraphicsContextLostException();
            if(EAGLContext.CurrentContext == Context)
                return Disposable.Empty;
            return MakeCurrent();
        }

        public bool IsSharedWith(IGlContext context) => context is GlContext other
            && ReferenceEquals(other.Context?.ShareGroup, Context?.ShareGroup);
        public bool CanCreateSharedContext => true;
        public IGlContext CreateSharedContext(IEnumerable<GlVersion> preferredVersions = null)
        {
            return new GlContext(GlInterface, Context.ShareGroup);
        }

        public GlVersion Version => EaglPlatformGraphics.GlVersion;
        public GlInterface GlInterface { get; }
        public int SampleCount
        {
            get
            {
                GlInterface.GetIntegerv(GlConsts.GL_SAMPLES, out var samples);
                return samples;
            }
        }
        public int StencilSize
        {
            get
            {
                GlInterface.GetIntegerv(GlConsts.GL_STENCIL_BITS, out var stencil);
                return stencil;
            }
        }

        public object TryGetFeature(Type featureType) => null;
    }
}
