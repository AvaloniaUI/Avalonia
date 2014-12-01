// -----------------------------------------------------------------------
// <copyright file="WindowsPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Win32
{
    using System;
    using System.Reactive.Disposables;
    using System.Threading;
    using System.Threading.Tasks;
    using Perspex.Input;
    using Perspex.Platform;
    using Perspex.Threading;
    using Perspex.Win32.Input;
    using Perspex.Win32.Interop;
    using Splat;

    public class Win32Platform : IPlatformThreadingInterface
    {
        private static Win32Platform instance = new Win32Platform();

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => WindowsKeyboardDevice.Instance, typeof(IKeyboardDevice));
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

            return Disposable.Create(() =>
            {
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
