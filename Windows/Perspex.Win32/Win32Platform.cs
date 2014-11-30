// -----------------------------------------------------------------------
// <copyright file="WindowsPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32
{
    using System;
    using System.Collections.Generic;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Threading;
    using Perspex.Win32.Input;
    using Perspex.Win32.Interop;
    using Perspex.Win32.Threading;
    using Splat;

    public class Win32Platform : IPlatformThreadingInterface
    {
        private static Win32Platform instance = new Win32Platform();

        private Dictionary<IntPtr, UnmanagedMethods.TimerProc> timerCallbacks =
            new Dictionary<IntPtr, UnmanagedMethods.TimerProc>();

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => WindowsKeyboardDevice.Instance, typeof(IKeyboardDevice));
            locator.Register(() => instance, typeof(IPlatformThreadingInterface));
        }

        public Dispatcher GetThreadDispatcher()
        {
            return WindowsDispatcher.GetThreadDispatcher();
        }


        public void KillTimer(object handle)
        {
            this.timerCallbacks.Remove((IntPtr)handle);
            UnmanagedMethods.KillTimer(IntPtr.Zero, (IntPtr)handle);
        }

        public object StartTimer(TimeSpan interval, Action callback)
        {
            UnmanagedMethods.TimerProc timerDelegate = (UnmanagedMethods.TimerProc)
                ((hWnd, uMsg, nIDEvent, dwTime) => callback());

            IntPtr handle = UnmanagedMethods.SetTimer(
                IntPtr.Zero,
                IntPtr.Zero,
                (uint)interval.TotalMilliseconds,
                timerDelegate);

            this.timerCallbacks.Add(handle, timerDelegate);

            return handle;
        }
    }
}
