// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Input.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Perspex.Input;
using Perspex.Platform;
using Perspex.Win32.Input;
using Perspex.Win32.Interop;
using Splat;

namespace Perspex.Win32
{
    public class Win32Platform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static Win32Platform s_instance = new Win32Platform();

        private UnmanagedMethods.WndProc _wndProcDelegate;

        private IntPtr _hwnd;

        private List<Delegate> _delegates = new List<Delegate>();

        public Win32Platform()
        {
            this.CreateMessageWindow();
        }

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

        private static void InitializeInternal()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new PopupImpl(), typeof(IPopupImpl));
            locator.Register(() => new ClipboardImpl(), typeof(IClipboard));
            locator.Register(() => WindowsKeyboardDevice.Instance, typeof(IKeyboardDevice));
            locator.Register(() => WindowsMouseDevice.Instance, typeof(IMouseDevice));
            locator.Register(() => CursorFactory.Instance, typeof(IStandardCursorFactory));
            locator.Register(() => s_instance, typeof(IPlatformSettings));
            locator.Register(() => s_instance, typeof(IPlatformThreadingInterface));
            locator.RegisterConstant(new AssetLoader(), typeof(IAssetLoader));
        }

        public static void Initialize()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new WindowImpl(), typeof(IWindowImpl));
            InitializeInternal();
        }

        public static void InitializeEmbedded()
        {
            var locator = Locator.CurrentMutable;
            locator.Register(() => new EmbeddedWindowImpl(), typeof(IWindowImpl));
            InitializeInternal();
        }

        public bool HasMessages()
        {
            UnmanagedMethods.MSG msg;
            return UnmanagedMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
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
            UnmanagedMethods.TimerProc timerDelegate =
                (hWnd, uMsg, nIDEvent, dwTime) => callback();

            IntPtr handle = UnmanagedMethods.SetTimer(
                IntPtr.Zero,
                IntPtr.Zero,
                (uint)interval.TotalMilliseconds,
                timerDelegate);

            // Prevent timerDelegate being garbage collected.
            _delegates.Add(timerDelegate);

            return Disposable.Create(() =>
            {
                _delegates.Remove(timerDelegate);
                UnmanagedMethods.KillTimer(IntPtr.Zero, handle);
            });
        }

        public void Wake()
        {
            //UnmanagedMethods.PostMessage(
            //    this.hwnd,
            //    (int)UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM,
            //    IntPtr.Zero,
            //    IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void CreateMessageWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(this.WndProc);

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf(typeof(UnmanagedMethods.WNDCLASSEX)),
                lpfnWndProc = _wndProcDelegate,
                hInstance = Marshal.GetHINSTANCE(this.GetType().Module),
                lpszClassName = "PerspexMessageWindow",
            };

            ushort atom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = UnmanagedMethods.CreateWindowEx(0, atom, null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }
    }
}
