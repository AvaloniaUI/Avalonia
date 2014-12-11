// -----------------------------------------------------------------------
// <copyright file="Win32Platform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Disposables;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Win32.Input;
    using Perspex.Win32.Interop;
    using Splat;

    public class Win32Platform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static Win32Platform instance = new Win32Platform();

        private List<Delegate> delegates = new List<Delegate>();

        public Size DoubleClickSize
        {
            get
            {
                return new Size(
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXDOUBLECLK),
                    UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYDOUBLECLK));
            }
        }

        public TimeSpan DoubleClickTime
        {
            get { return TimeSpan.FromMilliseconds(UnmanagedMethods.GetDoubleClickTime()); }
        }

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new WindowImpl(), typeof(IWindowImpl));
            locator.Register(() => WindowsKeyboardDevice.Instance, typeof(IKeyboardDevice));
            locator.Register(() => instance, typeof(IPlatformSettings));
            locator.Register(() => instance, typeof(IPlatformThreadingInterface));
        }

        public void ProcessMessage()
        {
            UnmanagedMethods.MSG msg;
            UnmanagedMethods.GetMessage(out msg, IntPtr.Zero, 0, 0);
            UnmanagedMethods.TranslateMessage(ref msg);
            UnmanagedMethods.DispatchMessage(ref msg);
        }

        public IDisposable StartTimer(TimeSpan interval, Action callback)
        {
            UnmanagedMethods.TimerProc timerDelegate = (UnmanagedMethods.TimerProc)
                ((hWnd, uMsg, nIDEvent, dwTime) => callback());

            IntPtr handle = UnmanagedMethods.SetTimer(
                IntPtr.Zero,
                IntPtr.Zero,
                (uint)interval.TotalMilliseconds,
                timerDelegate);

            // Prevent timerDelegate being garbage collected.
            this.delegates.Add(timerDelegate);

            return Disposable.Create(() =>
            {
                this.delegates.Remove(timerDelegate);
                UnmanagedMethods.KillTimer(IntPtr.Zero, handle);
            });
        }

        public void Wake()
        {
            UnmanagedMethods.PostMessage(
                IntPtr.Zero,
                (int)UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM,
                IntPtr.Zero,
                IntPtr.Zero);
        }
    }
}
