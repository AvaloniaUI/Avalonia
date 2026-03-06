#if NET7_0_OR_GREATER
global using ComVariantMarshaller = Avalonia.Win32.Automation.Marshalling.ComVariantMarshaller;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Marshalling;

[CustomMarshaller(typeof(object), MarshalMode.Default, typeof(ComVariantMarshaller))]
internal static class ComVariantMarshaller
{
    public static ComVariant ConvertToUnmanaged(object? managed) => ComVariant.Create(managed);

    public static object? ConvertToManaged(ComVariant unmanaged) => unmanaged.AsObject();

    public static void Free(ComVariant unmanaged) => unmanaged.Dispose();
}
#endif
