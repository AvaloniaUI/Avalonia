using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NImpeller;

namespace Avalonia.Impeller.Interop;

// NImpeller 3.50.0 can't pass a null backdrop filter to SaveLayer.
internal static unsafe partial class ImpellerNative
{
    private const string NImpellerAssembly = "NImpeller";

    [LibraryImport("impeller")]
    private static partial void ImpellerDisplayListBuilderSaveLayer(nint builder, ImpellerRect* bounds, nint paint,
        nint backdrop);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Handle")]
    [return: UnsafeAccessorType("NImpeller.ImpellerDisplayListBuilderHandle, " + NImpellerAssembly)]
    private static extern object GetHandle(ImpellerDisplayListBuilder builder);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Handle")]
    [return: UnsafeAccessorType("NImpeller.ImpellerPaintHandle, " + NImpellerAssembly)]
    private static extern object GetHandle(ImpellerPaint paint);

    public static void SaveLayer(this ImpellerDisplayListBuilder builder, ImpellerRect bounds, ImpellerPaint paint)
    {
        var builderHandle = (SafeHandle)GetHandle(builder);
        var paintHandle = (SafeHandle)GetHandle(paint);
        var addedBuilder = false;
        var addedPaint = false;
        try
        {
            builderHandle.DangerousAddRef(ref addedBuilder);
            paintHandle.DangerousAddRef(ref addedPaint);
            ImpellerDisplayListBuilderSaveLayer(builderHandle.DangerousGetHandle(), &bounds,
                paintHandle.DangerousGetHandle(), IntPtr.Zero);
        }
        finally
        {
            if (addedPaint)
                paintHandle.DangerousRelease();
            if (addedBuilder)
                builderHandle.DangerousRelease();
        }
    }
}
