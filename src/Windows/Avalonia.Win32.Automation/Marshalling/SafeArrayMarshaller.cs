#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Marshalling;

[CustomMarshaller(typeof(IReadOnlyList<>), MarshalMode.Default, typeof(SafeArrayMarshaller<>))]
internal static class SafeArrayMarshaller<T> where T : notnull
{
    public static SafeArrayRef ConvertToUnmanaged(IReadOnlyList<T>? managed) =>
        managed is null ? new SafeArrayRef()
        : SafeArrayRef.TryCreate(managed, out var result, out _) ? result.Value
        : throw new NotImplementedException($"SafeArray marshalling for '{managed?.GetType().Name}' is not implemented.");

    public static IReadOnlyList<T>? ConvertToManaged(SafeArrayRef unmanaged) => SafeArrayRef.ToReadOnlyList<T>(unmanaged);

    public static void Free(SafeArrayRef unmanaged) => unmanaged.Destroy();
}
#endif
