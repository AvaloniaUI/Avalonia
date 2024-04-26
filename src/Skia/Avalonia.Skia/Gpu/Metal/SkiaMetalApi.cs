using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Compatibility;
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
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, "SkiaSharp.GRContextOptionsNative", "SkiaSharp")]
    public SkiaMetalApi()
    {
        // Make sure that skia is loaded
        GC.KeepAlive(new SKPaint());

        // https://github.com/mono/SkiaSharp/blob/25e70a390e2128e5a54d28795365bf9fdaa7161c/binding/SkiaSharp/SkiaApi.cs#L9-L13
        // Note, IsIOS also returns true on MacCatalyst.
        var libSkiaSharpPath = OperatingSystemEx.IsIOS() || OperatingSystemEx.IsTvOS() ?
            "@rpath/libSkiaSharp.framework/libSkiaSharp" :
            "libSkiaSharp";
        var dll = NativeLibraryEx.Load(libSkiaSharpPath, typeof(SKPaint).Assembly);

        IntPtr address;

        if (NativeLibraryEx.TryGetExport(dll, "gr_direct_context_make_metal_with_options", out address))
        {
            _gr_direct_context_make_metal_with_options =
                (delegate* unmanaged[Stdcall] <IntPtr, IntPtr, IntPtr, IntPtr>)address;
        }
        else
        {
            throw new InvalidOperationException(
                "Unable to export gr_direct_context_make_metal_with_options. Make sure SkiaSharp is up to date.");
        }

        if(NativeLibraryEx.TryGetExport(dll, "gr_backendrendertarget_new_metal", out address))
        {
            _gr_backendrendertarget_new_metal =
                (delegate* unmanaged[Stdcall]<int, int, int, GRMtlTextureInfoNative*, IntPtr>)address;
        }
        else
        {
            throw new InvalidOperationException(
                "Unable to export gr_backendrendertarget_new_metal. Make sure SkiaSharp is up to date.");
        }

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

    [UnconditionalSuppressMessage("Trimming", "IL3050", Justification = "We have DynamicDependency above.")]
    public GRContext CreateContext(IntPtr device, IntPtr queue, GRContextOptions? options)
    {
        options ??= new();
        var nativeOptions = _contextOptionsToNative.Invoke(options, null)!;
        var pOptions = Marshal.AllocHGlobal(Marshal.SizeOf(nativeOptions));
        Marshal.StructureToPtr(nativeOptions, pOptions, false);
        var context = _gr_direct_context_make_metal_with_options(device, queue, pOptions);
        Marshal.FreeHGlobal(pOptions);
        if (context == IntPtr.Zero)
            throw new InvalidOperationException("Unable to create GRContext from Metal device.");
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
