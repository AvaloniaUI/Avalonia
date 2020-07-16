using System;
using Avalonia.Threading;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32
{
    internal class NonPumpingWaitProvider : AvaloniaSynchronizationContext.INonPumpingPlatformWaitProvider
    {
        public int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
        {
            return UnmanagedMethods.WaitForMultipleObjectsEx(waitHandles.Length, waitHandles, waitAll,
                millisecondsTimeout, false);
        }
    }
}
