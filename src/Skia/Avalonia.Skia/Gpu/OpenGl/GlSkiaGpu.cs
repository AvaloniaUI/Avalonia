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

            unsafe void Proxy(int size, IntPtr strings)
            {
                int* p = (int*)strings.ToPointer();
                
                for (int i = 0; i < size; i++)
                {
                    Console.WriteLine("Generate" + *p);
                    
                    p++;
                }
                _original(size, strings);
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

            unsafe void Proxy(int size, IntPtr strings)
            {
                int* p = (int*)strings.ToPointer();
                
                for (int i = 0; i < size; i++)
                {
                    Console.WriteLine("Delete" + *p);
                    
                    p++;
                }
                _original(size, strings);
            }
        }

        private static IntPtr GetProcAddress(string proc, Func<string, IntPtr> actualGetProcAddress)
        {
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
