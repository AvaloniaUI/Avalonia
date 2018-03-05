using System;
using Avalonia.Win32.Interop;
using Microsoft.Win32.SafeHandles;

namespace Avalonia.Win32
{
    internal class TimerHandle : CriticalHandleZeroOrMinusOneIsInvalid
    {
        private UnmanagedMethods.TimerProc _timerProc;

        public TimerHandle(IntPtr handle, UnmanagedMethods.TimerProc timerProc)
        {
            this.handle = handle;
            _timerProc = timerProc;
        }

        protected override bool ReleaseHandle()
        {
            _timerProc = null;
            return UnmanagedMethods.KillTimer(IntPtr.Zero, handle);
        }
    }
}
