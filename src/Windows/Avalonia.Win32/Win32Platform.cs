// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Input.Platform;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using Avalonia.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using System.IO;
#if NETSTANDARD
using Win32Exception = Avalonia.Win32.NetStandard.AvaloniaWin32Exception;
#else
using System.ComponentModel;
#endif

namespace Avalonia
{
    public static class Win32ApplicationExtensions
    {
        public static T UseWin32<T>(
            this T builder,
            bool deferredRendering = true) 
                where T : AppBuilderBase<T>, new()
        {
            return builder.UseWindowingSubsystem(
                () => Win32.Win32Platform.Initialize(deferredRendering),
                "Win32");
        }
    }
}

namespace Avalonia.Win32
{
    class Win32Platform : IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform, IPlatformIconLoader
    {
        private static readonly Win32Platform s_instance = new Win32Platform();
        private static uint _uiThread;
        private UnmanagedMethods.WndProc _wndProcDelegate;
        private IntPtr _hwnd;
        private readonly List<Delegate> _delegates = new List<Delegate>();

        public Win32Platform()
        {
            // Declare that this process is aware of per monitor DPI
            if (UnmanagedMethods.ShCoreAvailable)
            {
                UnmanagedMethods.SetProcessDpiAwareness(UnmanagedMethods.PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
            }

            CreateMessageWindow();
        }

        public static bool UseDeferredRendering { get; set; }

        public Size DoubleClickSize => new Size(
            UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXDOUBLECLK),
            UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYDOUBLECLK));

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(UnmanagedMethods.GetDoubleClickTime());

        public static void Initialize()
        {
            Initialize(true);
        }

        public static void Initialize(bool deferredRendering = true)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(WindowsKeyboardDevice.Instance)
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop(60))
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>()
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<IPlatformIconLoader>().ToConstant(s_instance);

            UseDeferredRendering = deferredRendering;
            _uiThread = UnmanagedMethods.GetCurrentThreadId();
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

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UnmanagedMethods.MSG msg;
                UnmanagedMethods.GetMessage(out msg, IntPtr.Zero, 0, 0);
                UnmanagedMethods.TranslateMessage(ref msg);
                UnmanagedMethods.DispatchMessage(ref msg);
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action callback)
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

        private static readonly int SignalW = unchecked((int) 0xdeadbeaf);
        private static readonly int SignalL = unchecked((int)0x12345678);

        public void Signal(DispatcherPriority prio)
        {
            UnmanagedMethods.PostMessage(
                _hwnd,
                (int) UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM,
                new IntPtr(SignalW),
                new IntPtr(SignalL));
        }

        public bool CurrentThreadIsLoopThread => _uiThread == UnmanagedMethods.GetCurrentThreadId();

        public event Action<DispatcherPriority?> Signaled;

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (int) UnmanagedMethods.WindowsMessage.WM_DISPATCH_WORK_ITEM && wParam.ToInt64() == SignalW && lParam.ToInt64() == SignalL)
            {
                Signaled?.Invoke(null);
            }
            return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void CreateMessageWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(WndProc);

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
                lpfnWndProc = _wndProcDelegate,
                hInstance = UnmanagedMethods.GetModuleHandle(null),
                lpszClassName = "AvaloniaMessageWindow " + Guid.NewGuid(),
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

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            var embedded = new EmbeddedWindowImpl();
            embedded.Show();
            return embedded;
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return CreateIconImpl(stream);
            }
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return CreateIconImpl(stream);
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                return new IconImpl(new System.Drawing.Bitmap(memoryStream));
            }
        }

        private static IconImpl CreateIconImpl(Stream stream)
        {
            try
            {
                return new IconImpl(new System.Drawing.Icon(stream));
            }
            catch (ArgumentException)
            {
                return new IconImpl(new System.Drawing.Bitmap(stream));
            }
        }

    }
}
