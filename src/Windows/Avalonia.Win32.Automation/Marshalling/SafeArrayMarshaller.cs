#if NET7_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Marshalling;

[CustomMarshaller(typeof(CustomMarshallerAttribute.GenericPlaceholder[]), MarshalMode.Default, typeof(SafeArrayMarshaller<>))]
internal static class SafeArrayMarshaller<T> where T : notnull
{
    public static SafeArrayRef ConvertToUnmanaged(T[]? managed) =>
        managed is null ? new SafeArrayRef()
        : SafeArrayRef.TryCreate(managed, out var result, out _) ? result.Value
        : throw new NotImplementedException($"SafeArray marshalling for '{managed?.GetType().Name}' is not implemented.");

    public static T[]? ConvertToManaged(SafeArrayRef unmanaged) => SafeArrayRef.ToArray<T>(unmanaged);

    public static void Free(SafeArrayRef unmanaged) => unmanaged.Destroy();
}
#endif
