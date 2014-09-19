// -----------------------------------------------------------------------
// <copyright file="WindowsPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Windows
{
    using System;
    using System.Collections.Generic;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Threading;
    using Perspex.Windows.Input;
    using Perspex.Windows.Interop;
    using Perspex.Windows.Threading;
    using Splat;

    public class WindowsPlatform : IPlatformThreadingInterface
    {
        private static WindowsPlatform instance = new WindowsPlatform();

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
