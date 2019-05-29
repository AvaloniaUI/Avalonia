// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia
{
    public static class Win32ApplicationExtensions
    {
        public static T UseWin32<T>(
            this T builder) 
                where T : AppBuilderBase<T>, new()
        {
            return builder.UseWindowingSubsystem(
                () => Win32.Win32Platform.Initialize(
                    AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions()),
                "Win32");
        }
    }

    public class Win32PlatformOptions
    {
        public bool UseDeferredRendering { get; set; } = true;
        public bool AllowEglInitialization { get; set; }
        public bool? EnableMultitouch { get; set; }
    }
}

namespace Avalonia.Win32
{
    class Win32Platform : IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform, IPlatformIconLoader
    {
        private static readonly Win32Platform s_instance = new Win32Platform();
        private static Thread _uiThread;
        private UnmanagedMethods.WndProc _wndProcDelegate;
        private IntPtr _hwnd;
        private readonly List<Delegate> _delegates = new List<Delegate>();

        public Win32Platform()
        {
            SetDpiAwareness();
            CreateMessageWindow();
        }

        public static bool UseDeferredRendering => Options.UseDeferredRendering;
        public static Win32PlatformOptions Options { get; private set; }

        public Size DoubleClickSize => new Size(
            UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CXDOUBLECLK),
            UnmanagedMethods.GetSystemMetrics(UnmanagedMethods.SystemMetric.SM_CYDOUBLECLK));

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(UnmanagedMethods.GetDoubleClickTime());

        public static void Initialize()
        {
            Initialize(new Win32PlatformOptions());
        }

        public static void Initialize(Win32PlatformOptions options)
        {
            Options = options;
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(WindowsKeyboardDevice.Instance)
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new RenderTimer(60))
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>()
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IPlatformIconLoader>().ToConstant(s_instance);
            if (options.AllowEglInitialization)
                Win32GlManager.Initialize();
            
            _uiThread = Thread.CurrentThread;

            if (OleContext.Current != null)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformDragSource>().ToSingleton<DragSource>();
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

        public bool CurrentThreadIsLoopThread => _uiThread == Thread.CurrentThread;

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

        private static void SetDpiAwareness()
        {
            // Ideally we'd set DPI awareness in the manifest but this doesn't work for netcoreapp2.0
            // apps as they are actually dlls run by a console loader. Instead we have to do it in code,
            // but there are various ways to do this depending on the OS version.
            var user32 = LoadLibrary("user32.dll");
            var method = GetProcAddress(user32, nameof(SetProcessDpiAwarenessContext));

            if (method != IntPtr.Zero)
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) ||
                    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE))
                {
                    return;
                }
            }

            var shcore = LoadLibrary("shcore.dll");
            method = GetProcAddress(shcore, nameof(SetProcessDpiAwareness));

            if (method != IntPtr.Zero)
            {
                SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE);
                return;
            }

            SetProcessDPIAware();
        }
    }
}
