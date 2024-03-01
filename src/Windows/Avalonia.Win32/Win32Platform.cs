using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Avalonia.Reactive;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia
{
    public static class Win32ApplicationExtensions
    {
        public static AppBuilder UseWin32(this AppBuilder builder)
        {
            return builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(
                () => Win32.Win32Platform.Initialize(
                    AvaloniaLocator.Current.GetService<Win32PlatformOptions>() ?? new Win32PlatformOptions()),
                "Win32");
        }
    }
}

namespace Avalonia.Win32
{
    internal class Win32Platform : IWindowingPlatform, IPlatformIconLoader, IPlatformLifetimeEventsImpl
    {
        private static readonly Win32Platform s_instance = new();
        private static Win32PlatformOptions? s_options;
        private static Compositor? s_compositor;
        internal const int TIMERID_DISPATCHER = 1;

        private WndProc? _wndProcDelegate;
        private IntPtr _hwnd;
        private Win32DispatcherImpl _dispatcher;

        public Win32Platform()
        {
            CreateMessageWindow();
            _dispatcher = new Win32DispatcherImpl(_hwnd);
        }

        internal static Win32Platform Instance => s_instance;
        internal static IPlatformSettings PlatformSettings => AvaloniaLocator.Current.GetRequiredService<IPlatformSettings>();

        internal IntPtr Handle => _hwnd;

        /// <summary>
        /// Gets the actual WindowsVersion. Same as the info returned from RtlGetVersion.
        /// </summary>
        public static Version WindowsVersion { get; } = RtlGetVersion();

        internal static bool UseOverlayPopups => Options.OverlayPopups;

        public static Win32PlatformOptions Options
            => s_options ?? throw new InvalidOperationException($"{nameof(Win32Platform)} hasn't been initialized");

        internal static Compositor Compositor
            => s_compositor ?? throw new InvalidOperationException($"{nameof(Win32Platform)} hasn't been initialized");

        public static void Initialize()
        {
            Initialize(new Win32PlatformOptions());
        }

        public static void Initialize(Win32PlatformOptions options)
        {
            s_options = options;

            SetDpiAwareness();

            var renderTimer = options.ShouldRenderOnUIThread ? new UiThreadRenderTimer(60) : new DefaultRenderTimer(60);

            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<ICursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(WindowsKeyboardDevice.Instance)
                .Bind<IPlatformSettings>().ToSingleton<Win32PlatformSettings>()
                .Bind<IDispatcherImpl>().ToConstant(s_instance._dispatcher)
                .Bind<IRenderTimer>().ToConstant(renderTimer)
                .Bind<IWindowingPlatform>().ToConstant(s_instance)
                .Bind<PlatformHotkeyConfiguration>().ToConstant(new PlatformHotkeyConfiguration(KeyModifiers.Control)
                {
                    OpenContextMenu =
                    {
                        // Add Shift+F10
                        new KeyGesture(Key.F10, KeyModifiers.Shift)
                    }
                })
                .Bind<IPlatformIconLoader>().ToConstant(s_instance)
                .Bind<NonPumpingLockHelper.IHelperImpl>().ToConstant(NonPumpingWaitHelperImpl.Instance)
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new WindowsMountedVolumeInfoProvider())
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(s_instance);

            IPlatformGraphics? platformGraphics;
            if (options.CustomPlatformGraphics is not null)
            {
                if (options.CompositionMode?.Contains(Win32CompositionMode.RedirectionSurface) == false)
                {
                    throw new InvalidOperationException(
                        $"{nameof(Win32PlatformOptions)}.{nameof(Win32PlatformOptions.CustomPlatformGraphics)} is only " +
                        $"compatible with {nameof(Win32CompositionMode)}.{nameof(Win32CompositionMode.RedirectionSurface)}");
                }
                
                platformGraphics = options.CustomPlatformGraphics;
            }
            else
            {
                platformGraphics = Win32GlManager.Initialize();   
            }
            
            if (OleContext.Current != null)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformDragSource>().ToSingleton<DragSource>();
            
            s_compositor = new Compositor( platformGraphics);
            AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(s_compositor);
        }
        
        public event EventHandler<ShutdownRequestedEventArgs>? ShutdownRequested;

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (int)WindowsMessage.WM_DISPATCH_WORK_ITEM 
                && wParam.ToInt64() == Win32DispatcherImpl.SignalW 
                && lParam.ToInt64() == Win32DispatcherImpl.SignalL) 
                _dispatcher?.DispatchWorkItem();

            if(msg == (uint)WindowsMessage.WM_QUERYENDSESSION)
            {
                if (ShutdownRequested != null)
                {
                    var e = new ShutdownRequestedEventArgs();

                    ShutdownRequested(this, e);

                    if(e.Cancel)
                    {
                        return IntPtr.Zero;
                    }
                }
            }

            if (msg == (uint)WindowsMessage.WM_SETTINGCHANGE 
                && PlatformSettings is Win32PlatformSettings win32PlatformSettings)
            {
                var changedSetting = Marshal.PtrToStringAuto(lParam);
                if (changedSetting == "ImmersiveColorSet" // dark/light mode
                    || changedSetting == "WindowsThemeElement") // high contrast mode
                {
                    win32PlatformSettings.OnColorValuesChanged();   
                }
            }

            if (msg == (uint)WindowsMessage.WM_TIMER)
            {
                if (wParam == (IntPtr)TIMERID_DISPATCHER)
                    _dispatcher?.FireTimer();
            }
            
            TrayIconImpl.ProcWnd(hWnd, msg, wParam, lParam);

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void CreateMessageWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = WndProc;

            WNDCLASSEX wndClassEx = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = _wndProcDelegate,
                hInstance = GetModuleHandle(null),
                lpszClassName = "AvaloniaMessageWindow " + Guid.NewGuid(),
            };

            ushort atom = RegisterClassEx(ref wndClassEx);

            if (atom == 0)
            {
                throw new Win32Exception();
            }

            _hwnd = CreateWindowEx(0, atom, null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (_hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return new TrayIconImpl();
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl();
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            var embedded = new EmbeddedWindowImpl();
            embedded.Show(true, false);
            return embedded;
        }

        public IWindowIconImpl LoadIcon(string fileName)
        {
            using (var stream = File.OpenRead(fileName))
            {
                return new IconImpl(stream);
            }
        }

        public IWindowIconImpl LoadIcon(Stream stream)
        {
            return new IconImpl(stream);
        }

        public IWindowIconImpl LoadIcon(IBitmapImpl bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream);
                return new IconImpl(memoryStream);
            }
        }

        private static void SetDpiAwareness()
        {
            // Ideally we'd set DPI awareness in the manifest but this doesn't work for netcoreapp2.0
            // apps as they are actually dlls run by a console loader. Instead we have to do it in code,
            // but there are various ways to do this depending on the OS version.
            var user32 = LoadLibrary("user32.dll");
            var method = GetProcAddress(user32, nameof(SetProcessDpiAwarenessContext));

            var dpiAwareness = Options.DpiAwareness;

            if (method != IntPtr.Zero)
            {
                if (dpiAwareness == Win32DpiAwareness.Unaware)
                {
                    if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_UNAWARE))
                    {
                        return;
                    }
                }
                else if (dpiAwareness == Win32DpiAwareness.SystemDpiAware)
                {
                    if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_SYSTEM_AWARE))
                    {
                        return;
                    }
                }
                else if (dpiAwareness == Win32DpiAwareness.PerMonitorDpiAware)
                {
                    if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) ||
                    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE))
                    {
                        return;
                    }
                }
            }

            var shcore = LoadLibrary("shcore.dll");
            method = GetProcAddress(shcore, nameof(SetProcessDpiAwareness));

            if (method != IntPtr.Zero)
            {
                var awareness = (dpiAwareness) switch
                {
                    Win32DpiAwareness.Unaware => PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE,
                    Win32DpiAwareness.SystemDpiAware => PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE,
                    Win32DpiAwareness.PerMonitorDpiAware => PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE,
                    _ => PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE,
                };

                SetProcessDpiAwareness(awareness);
                return;
            }

            if (dpiAwareness != Win32DpiAwareness.Unaware)
                SetProcessDPIAware();
        }
    }
}
