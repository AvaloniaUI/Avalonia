using System;
using System.Runtime.InteropServices;

namespace Avalonia.Impeller;

internal sealed partial class ObjCAutoreleasePool : IDisposable
{
    private IntPtr _pool = Push();

    [LibraryImport("libobjc", EntryPoint = "objc_autoreleasePoolPush")]
    private static partial IntPtr Push();

    [LibraryImport("libobjc", EntryPoint = "objc_autoreleasePoolPop")]
    private static partial void Pop(IntPtr pool);

    public void Dispose()
    {
        if (_pool == IntPtr.Zero)
            return;
        Pop(_pool);
        _pool = IntPtr.Zero;
    }
}
