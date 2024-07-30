#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Marshalling;

[CustomMarshaller(typeof(IReadOnlyList<>), MarshalMode.Default, typeof(SafeArrayMarshaller<>))]
internal static unsafe class SafeArrayMarshaller<T> where T : notnull
{
    public static SafeArray* ConvertToUnmanaged(IReadOnlyList<T>? managed) =>
        managed is null ? null
        : SafeArray.TryCreate(managed, out var result, out _) ? result
        : throw new NotImplementedException($"SafeArray marshalling for '{managed?.GetType().Name}' is not implemented.");

    public static IReadOnlyList<T>? ConvertToManaged(SafeArray* unmanaged) => SafeArray.ToReadOnlyList<T>(unmanaged);

    public static void Free(SafeArray* unmanaged)
    {
        if (unmanaged is not null)
        {
            SafeArray.SafeArrayDestroy(unmanaged);
        }
    }
}
#endif
