using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Platform.Interop;
using SkiaSharp;
using BindingFlags = System.Reflection.BindingFlags;

namespace Avalonia.Skia.Metal;

internal unsafe class SkiaMetalApi
{
    delegate* unmanaged[Stdcall] <IntPtr, IntPtr, IntPtr, IntPtr> _gr_direct_context_make_metal_with_options;
    private delegate* unmanaged[Stdcall]<int, int, int, GRMtlTextureInfoNative*, IntPtr>
        _gr_backendrendertarget_new_metal;
    private readonly ConstructorInfo _contextCtor;
    private readonly MethodInfo _contextOptionsToNative;
    private readonly ConstructorInfo _renderTargetCtor;
    
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(GRContext))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicConstructors, typeof(GRBackendRenderTarget))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(GRContextOptions))]
    public SkiaMetalApi()
    {
        // Make sure that skia is loaded
        GC.KeepAlive(new SKPaint());
        
        var loader = AvaloniaLocator.Current.GetRequiredService<IDynamicLibraryLoader>();
#if NET6_0_OR_GREATER
        var dll = NativeLibrary.Load("libSkiaSharp", typeof(SKPaint).Assembly, null);
#else
        var dll = loader.LoadLibrary("libSkiaSharp");
#endif
        _gr_direct_context_make_metal_with_options = (delegate* unmanaged[Stdcall] <IntPtr, IntPtr, IntPtr, IntPtr>)
            loader.GetProcAddress(dll, "gr_direct_context_make_metal_with_options", false);
        _gr_backendrendertarget_new_metal =
            (delegate* unmanaged[Stdcall]<int, int, int, GRMtlTextureInfoNative*, IntPtr>)
            loader.GetProcAddress(dll, "gr_backendrendertarget_new_metal", false);
        
        _contextCtor = typeof(GRContext).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(IntPtr), typeof(bool) }, null) ?? throw new MissingMemberException("GRContext.ctor(IntPtr,bool)");

                
        _renderTargetCtor = typeof(GRBackendRenderTarget).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(IntPtr), typeof(bool) }, null) ?? throw new MissingMemberException("GRContext.ctor(IntPtr,bool)");

        _contextOptionsToNative = typeof(GRContextOptions).GetMethod("ToNative",
                                      BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                  ?? throw new MissingMemberException("GRContextOptions.ToNative()");
    }

    public GRContext CreateContext(IntPtr device, IntPtr queue, GRContextOptions? options)
    {
        options ??= new();
        var nativeOptions = _contextOptionsToNative.Invoke(options, null)!;
        var pOptions = Marshal.AllocHGlobal(Marshal.SizeOf(nativeOptions));
        Marshal.StructureToPtr(nativeOptions, pOptions, false);
        var context = _gr_direct_context_make_metal_with_options(device, queue, pOptions);
        Marshal.FreeHGlobal(pOptions);
        if (context == IntPtr.Zero)
            throw new ArgumentException();
        return (GRContext)_contextCtor.Invoke(new object[] { context, true });
    }

    internal struct GRMtlTextureInfoNative
    {
        public IntPtr Texture;
    }

    public GRBackendRenderTarget CreateBackendRenderTarget(int width, int height, int samples, IntPtr texture)
    {
        var info = new GRMtlTextureInfoNative() { Texture = texture };
        var target = _gr_backendrendertarget_new_metal(width, height, samples, &info);
        if (target == IntPtr.Zero)
            throw new InvalidOperationException("Unable to create GRBackendRenderTarget");
        return (GRBackendRenderTarget)_renderTargetCtor.Invoke(new object[] { target, true });
    }
}
