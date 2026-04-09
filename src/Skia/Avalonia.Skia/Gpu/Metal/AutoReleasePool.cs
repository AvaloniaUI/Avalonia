using System;
using System.Runtime.InteropServices;

namespace Avalonia.Skia.Metal;

internal sealed partial class AutoReleasePool : IDisposable
{
    private IntPtr _pool;

    public AutoReleasePool()
    {
        _pool = Push();
    }

    public void Dispose()
    {
        var pool = _pool;
        if (pool != IntPtr.Zero)
        {
            _pool = IntPtr.Zero;
            Pop(pool);
        }
    }

    [LibraryImport("libobjc", EntryPoint = "objc_autoreleasePoolPush")]
    private static partial IntPtr Push();

    [LibraryImport("libobjc", EntryPoint = "objc_autoreleasePoolPop")]
    private static partial void Pop(IntPtr pool);
}
