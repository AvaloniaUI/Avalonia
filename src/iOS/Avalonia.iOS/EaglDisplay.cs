using System;
using System.Reactive.Disposables;
using Avalonia.OpenGL;
using OpenGLES;

namespace Avalonia.iOS
{
    class EaglFeature : IPlatformOpenGlInterface
    {
        public IGlContext PrimaryContext => Context;
        public IGlContext CreateSharedContext() => throw new NotSupportedException();
        public bool CanShareContexts => false;
        public bool CanCreateContexts => false;
        public IGlContext CreateContext() => throw new System.NotSupportedException();
        public GlContext Context { get; } = new GlContext();
    }

    class GlContext : IGlContext
    {
        public EAGLContext Context { get; private set; }
        
        public GlContext()
        {
            const string path = "/System/Library/Frameworks/OpenGLES.framework/OpenGLES";
            var libGl = ObjCRuntime.Dlfcn.dlopen(path, 1);
            if (libGl == IntPtr.Zero)
                throw new OpenGlException("Unable to load " + path);
            GlInterface = new GlInterface(Version, proc => ObjCRuntime.Dlfcn.dlsym(libGl, proc));
            Context = new EAGLContext(EAGLRenderingAPI.OpenGLES3);
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
            var old = EAGLContext.CurrentContext;
            if (!EAGLContext.SetCurrentContext(Context))
                throw new OpenGlException("Unable to make context current");
            return new ResetContext(old);
        }

        public IDisposable EnsureCurrent()
        {
            if(EAGLContext.CurrentContext == Context)
                return Disposable.Empty;
            return MakeCurrent();
        }

        public bool IsSharedWith(IGlContext context) => false;

        public GlVersion Version { get; } = new GlVersion(GlProfileType.OpenGLES, 3, 0);
        public GlInterface GlInterface { get; }
        public int SampleCount { get; } = 0;
        public int StencilSize { get; } = 9;
    }
}
