using System;
using System.Collections.Generic;
using Avalonia.OpenGL;
using OpenTK;

namespace Avalonia.OpenTK
{
    public class AvaloniaOpenTKIntegration
    {
        static GlVersion? g_GlVersion;
        
        static IGlContext Create(IGlContext shareWith, IList<GlVersion> probeVersions)
        {
            var feature = AvaloniaLocator.Current.GetService<IPlatformOpenGlInterface>();
            if (feature == null)
                throw new PlatformNotSupportedException("GL feature isn't enabled");
            
            if (feature.CanShareContexts)
                return feature.CreateContext(shareWith ?? feature.PrimaryContext, probeVersions);
            
            return feature.CreateOSTextureSharingCompatibleContext(shareWith, probeVersions);
        }

        public static bool IsAvailable => g_GlVersion.HasValue;

        public static IGlContext CreateCompatibleContext(IGlContext shareWith)
        {
            if (!g_GlVersion.HasValue)
                throw new InvalidOperationException("OpenTK integration is not initialized");

            return Create(shareWith, new[] { g_GlVersion.Value });
        }

        class AvaloniaBindingsContext : IBindingsContext
        {
            private readonly GlInterface _gl;

            public AvaloniaBindingsContext(GlInterface gl)
            {
                _gl = gl;
            }

            public IntPtr GetProcAddress(string procName) => _gl.GetProcAddress(procName);
        }

        internal static void Initialize(IList<GlVersion> probeVersions)
        {
            using var ctx = Create(null, probeVersions);
            using (ctx.MakeCurrent())
            {
                var bindings = new AvaloniaBindingsContext(ctx.GlInterface);
                if (ctx.Version.Type == GlProfileType.OpenGL)
                {
                    global::OpenTK.Graphics.OpenGL.GL.LoadBindings(bindings);
                    if (ctx.Version.Major >= 4)
                        global::OpenTK.Graphics.OpenGL4.GL.LoadBindings(bindings);
                }
                else
                {
                    global::OpenTK.Graphics.ES11.GL.LoadBindings(bindings);
                    if (ctx.Version.Major >= 2)
                        global::OpenTK.Graphics.ES20.GL.LoadBindings(bindings);
                    if (ctx.Version.Major >= 3)
                        global::OpenTK.Graphics.ES30.GL.LoadBindings(bindings);
                }

                if (bindings.GetProcAddress("wglCreateContextAttribsARB") != IntPtr.Zero)
                    global::OpenTK.Graphics.Wgl.Wgl.LoadBindings(bindings);

                g_GlVersion = ctx.Version;
            }
        }
    }
}
