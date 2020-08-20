using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Imaging;
using SkiaSharp;

namespace Avalonia.Skia
{
    class GlSkiaGpu : IOpenGlAwareSkiaGpu
    {
        private GRContext _grContext;
        private static List<int> _aliveTextures = new List<int>();
        private static IGlContext _glContext;

        class GenTexturesProxy
        {
            private GlGenTexturesDelegate _original;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void GlGenTexturesDelegate(int size, IntPtr strings);
            public GenTexturesProxy(IntPtr original)
            {
                _original = Marshal.GetDelegateForFunctionPointer<GlGenTexturesDelegate>(original);
                GlGenTexturesDelegate del = Proxy;
                GCHandle.Alloc(del);
                Pointer = Marshal.GetFunctionPointerForDelegate(del);
            }

            public IntPtr Pointer { get; }

            unsafe void Proxy(int size, IntPtr textureIds)
            {
                _original(size, textureIds);
                
                int* p = (int*)textureIds.ToPointer();
                
                for (int i = 0; i < size; i++)
                {
                    Console.WriteLine($"glGenTexture: {*p} - ({_aliveTextures.Count})");
                    if (GlSkiaGpu._aliveTextures.Contains(*p))
                    {
                        Console.WriteLine("Trying to add multiple textures with same id???");
                    }
                    else
                    {
                        GlSkiaGpu._aliveTextures.Add(*p);
                        _glContext.GlInterface.BindTexture(GlConsts.GL_TEXTURE_2D, *p);

                        int param = 0;
                        var pParam = new IntPtr(&param);
                        _glContext.GlInterface.GetTextureLevelParameteriv(*p, 0,
                            GlConsts.GL_TEXTURE_WIDTH, pParam);
                    }

                    p++;
                }
            }
        }
        
        class DeleteTexturesProxy
        {
            private GlDeleteTexturesDelegate _original;

            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            delegate void GlDeleteTexturesDelegate(int size, IntPtr strings);
            public DeleteTexturesProxy(IntPtr original)
            {
                _original = Marshal.GetDelegateForFunctionPointer<GlDeleteTexturesDelegate>(original);
                GlDeleteTexturesDelegate del = Proxy;
                GCHandle.Alloc(del);
                Pointer = Marshal.GetFunctionPointerForDelegate(del);
            }

            public IntPtr Pointer { get; }

            unsafe void Proxy(int size, IntPtr textureIds)
            {
                _original(size, textureIds);
                
                int* p = (int*)textureIds.ToPointer();

                for (int i = 0; i < size; i++)
                {
                    if (GlSkiaGpu._aliveTextures.Contains(*p))
                    {
                        _aliveTextures.Remove(*p);
                    }
                    else
                    {
                        Console.WriteLine("Deleting unknown texture");
                    }
                    
                    Console.WriteLine($"glDeleteTexture: {*p} - ({_aliveTextures.Count})");
                    
                    p++;
                }
            }
        }

        private static IntPtr GetProcAddress(string proc, Func<string, IntPtr> actualGetProcAddress)
        {
            Console.WriteLine(proc);
            switch (proc)
            {
                case "glGenTextures" :
                    return new GenTexturesProxy(actualGetProcAddress(proc)).Pointer;

                case "glDeleteTextures":
                    return new DeleteTexturesProxy(actualGetProcAddress(proc)).Pointer;
                
                default:
                    return actualGetProcAddress(proc);
                    
            }
        }

        public GlSkiaGpu(IWindowingPlatformGlFeature gl, long? maxResourceBytes)
        {
            var context = gl.MainContext;
            using (context.MakeCurrent())
            {
                using (var iface = context.Version.Type == GlProfileType.OpenGL ?
                    GRGlInterface.CreateOpenGl(proc => GetProcAddress(proc, context.GlInterface.GetProcAddress)) :
                    GRGlInterface.CreateGles(proc => GetProcAddress(proc, context.GlInterface.GetProcAddress)))
                {
                    _grContext = GRContext.CreateGl(iface);
                    _glContext = context;
                    if (maxResourceBytes.HasValue)
                    {
                        _grContext.SetResourceCacheLimit(maxResourceBytes.Value);
                    }
                }
            }
        }

        public ISkiaGpuRenderTarget TryCreateRenderTarget(IEnumerable<object> surfaces)
        {
            foreach (var surface in surfaces)
            {
                if (surface is IGlPlatformSurface glSurface)
                {
                    return new GlRenderTarget(_grContext, glSurface);
                }
            }

            return null;
        }

        public IOpenGlTextureBitmapImpl CreateOpenGlTextureBitmap() => new OpenGlTextureBitmapImpl();
    }
}
