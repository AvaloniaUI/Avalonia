using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Avalonia.Utilities;
using Avalonia.Win32.Input;
using Avalonia.Win32.Interop;
using JetBrains.Annotations;
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

    /// <summary>
    /// Platform-specific options which apply to Windows.
    /// </summary>
    public class Win32PlatformOptions
    {
        /// <summary>
        /// Deferred renderer would be used when set to true. Immediate renderer when set to false. The default value is true.
        /// </summary>
        /// <remarks>
        /// Avalonia has two rendering modes: Immediate and Deferred rendering.
        /// Immediate re-renders the whole scene when some element is changed on the scene. Deferred re-renders only changed elements.
        /// </remarks>
        public bool UseDeferredRendering { get; set; } = true;

        public bool UseCompositor { get; set; } = true;

        /// <summary>
        /// Enables ANGLE for Windows. For every Windows version that is above Windows 7, the default is true otherwise it's false.
        /// </summary>
        /// <remarks>
        /// GPU rendering will not be enabled if this is set to false.
        /// </remarks>
        public bool? AllowEglInitialization { get; set; }

        /// <summary>
        /// Embeds popups to the window when set to true. The default value is false.
        /// </summary>
        public bool OverlayPopups { get; set; }

        /// <summary>
        /// Avalonia would try to use native Widows OpenGL when set to true. The default value is false.
        /// </summary>
        public bool UseWgl { get; set; }

        public IList<GlVersion> WglProfiles { get; set; } = new List<GlVersion>
        {
            new GlVersion(GlProfileType.OpenGL, 4, 0),
            new GlVersion(GlProfileType.OpenGL, 3, 2),
        };

        /// <summary>
        /// Render Avalonia to a Texture inside the Windows.UI.Composition tree.
        /// </summary>
        /// <remarks>
        /// Supported on Windows 10 build 16299 and above. Ignored on other versions.
        /// This is recommended if you need to use AcrylicBlur or acrylic in your applications.
        /// </remarks>
        public bool UseWindowsUIComposition { get; set; } = true;

        /// <summary>
        /// When <see cref="UseWindowsUIComposition"/> enabled, create rounded corner blur brushes
        /// If set to null the brushes will be created using default settings (sharp corners)
        /// This can be useful when you need a rounded-corner blurred Windows 10 app, or borderless Windows 11 app
        /// </summary>
        public float? CompositionBackdropCornerRadius { get; set; }

        /// <summary>
        /// When <see cref="UseLowLatencyDxgiSwapChain"/> is active, renders Avalonia through a low-latency Dxgi Swapchain.
        /// Requires Feature Level 11_3 to be active, Windows 8.1+ Any Subversion. 
        /// This is only recommended if low input latency is desirable, and there is no need for the transparency
        /// and stylings / blurrings offered by <see cref="UseWindowsUIComposition"/><br/>
        /// This is mutually exclusive with 
        /// <see cref="UseWindowsUIComposition"/> which if active will override this setting. 
        /// </summary>
        public bool UseLowLatencyDxgiSwapChain { get; set; } = false;
        
        /// <summary>
        /// Provides a way to use a custom-implemented graphics context such as a custom ISkiaGpu
        /// </summary>
        [CanBeNull] public IPlatformGraphics CustomPlatformGraphics { get; set; }
    }
}

namespace Avalonia.Win32
{
    public class Win32Platform : IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform, IPlatformIconLoader, IPlatformLifetimeEventsImpl
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

        internal static Win32Platform Instance => s_instance;

        internal IntPtr Handle => _hwnd;

        /// <summary>
        /// Gets the actual WindowsVersion. Same as the info returned from RtlGetVersion.
        /// </summary>
        public static Version WindowsVersion { get; } = RtlGetVersion();

        public static bool UseDeferredRendering => Options.UseDeferredRendering;
        internal static bool UseOverlayPopups => Options.OverlayPopups;
        public static Win32PlatformOptions Options { get; private set; }
        
        internal static Compositor Compositor { get; private set; }
        internal static PlatformRenderInterfaceContextManager RenderInterface { get; private set; }

        public static void Initialize()
        {
            Initialize(new Win32PlatformOptions());
        }

        public static void Initialize(Win32PlatformOptions options)
        {
            Options = options;
            AvaloniaLocator.CurrentMutable
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<ICursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToConstant(WindowsKeyboardDevice.Instance)
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new DefaultRenderTimer(60))
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
                .Bind<NonPumpingLockHelper.IHelperImpl>().ToConstant(new NonPumpingSyncContext.HelperImpl())
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new WindowsMountedVolumeInfoProvider())
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(s_instance);
            
            _uiThread = Thread.CurrentThread;

            var platformGraphics = options?.CustomPlatformGraphics
                                   ?? Win32GlManager.Initialize();
            
            if (OleContext.Current != null)
                AvaloniaLocator.CurrentMutable.Bind<IPlatformDragSource>().ToSingleton<DragSource>();

            if (Options.UseCompositor)
                Compositor = new Compositor(AvaloniaLocator.Current.GetRequiredService<IRenderLoop>(), platformGraphics);
            else
                RenderInterface = new PlatformRenderInterfaceContextManager(platformGraphics);
        }

        public bool HasMessages()
        {
            UnmanagedMethods.MSG msg;
            return PeekMessage(out msg, IntPtr.Zero, 0, 0, 0);
        }

        public void ProcessMessage()
        {

            if (GetMessage(out var msg, IntPtr.Zero, 0, 0) > -1)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            else
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, Logging.LogArea.Win32Platform)
                    ?.Log(this, "Unmanaged error in {0}. Error Code: {1}", nameof(ProcessMessage), Marshal.GetLastWin32Error());

            }
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            var result = 0;
            while (!cancellationToken.IsCancellationRequested 
                && (result = GetMessage(out var msg, IntPtr.Zero, 0, 0)) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            if (result < 0)
            {
                Logging.Logger.TryGet(Logging.LogEventLevel.Error, Logging.LogArea.Win32Platform)
                    ?.Log(this, "Unmanaged error in {0}. Error Code: {1}", nameof(RunLoop), Marshal.GetLastWin32Error());
            }
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action callback)
        {
            UnmanagedMethods.TimerProc timerDelegate =
                (hWnd, uMsg, nIDEvent, dwTime) => callback();

            IntPtr handle = SetTimer(
                IntPtr.Zero,
                IntPtr.Zero,
                (uint)interval.TotalMilliseconds,
                timerDelegate);

            // Prevent timerDelegate being garbage collected.
            _delegates.Add(timerDelegate);

            return Disposable.Create(() =>
            {
                _delegates.Remove(timerDelegate);
                KillTimer(IntPtr.Zero, handle);
            });
        }

        private const int SignalW = unchecked((int)0xdeadbeaf);
        private const int SignalL = unchecked((int)0x12345678);

        public void Signal(DispatcherPriority prio)
        {
            PostMessage(
                _hwnd,
                (int)WindowsMessage.WM_DISPATCH_WORK_ITEM,
                new IntPtr(SignalW),
                new IntPtr(SignalL));
        }

        public bool CurrentThreadIsLoopThread => _uiThread == Thread.CurrentThread;

        public event Action<DispatcherPriority?> Signaled;

        public event EventHandler<ShutdownRequestedEventArgs> ShutdownRequested;

        [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Using Win32 naming for consistency.")]
        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == (int)WindowsMessage.WM_DISPATCH_WORK_ITEM && wParam.ToInt64() == SignalW && lParam.ToInt64() == SignalL)
            {
                Signaled?.Invoke(null);
            }

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
            
            TrayIconImpl.ProcWnd(hWnd, msg, wParam, lParam);

            return DefWindowProc(hWnd, msg, wParam, lParam);
        }

        private void CreateMessageWindow()
        {
            // Ensure that the delegate doesn't get garbage collected by storing it as a field.
            _wndProcDelegate = new UnmanagedMethods.WndProc(WndProc);

            UnmanagedMethods.WNDCLASSEX wndClassEx = new UnmanagedMethods.WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
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

        public ITrayIconImpl CreateTrayIcon ()
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
                return CreateIconImpl(memoryStream);
            }
        }

        private static IconImpl CreateIconImpl(Stream stream)
        {
            try
            {
                // new Icon() will work only if stream is an "ico" file.
                return new IconImpl(new System.Drawing.Icon(stream));
            }
            catch (ArgumentException)
            {
                // Fallback to Bitmap creation and converting into a windows icon. 
                using var icon = new System.Drawing.Bitmap(stream);
                var hIcon = icon.GetHicon();
                return new IconImpl(System.Drawing.Icon.FromHandle(hIcon));
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

        Size IPlatformSettings.GetTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(10, 10),
                _ => new(GetSystemMetrics(SystemMetric.SM_CXDRAG), GetSystemMetrics(SystemMetric.SM_CYDRAG)),
            };
        }

        Size IPlatformSettings.GetDoubleTapSize(PointerType type)
        {
            return type switch
            {
                PointerType.Touch => new(16, 16),
                _ => new(GetSystemMetrics(SystemMetric.SM_CXDOUBLECLK), GetSystemMetrics(SystemMetric.SM_CYDOUBLECLK)),
            };
        }

        TimeSpan IPlatformSettings.GetDoubleTapTime(PointerType type) => TimeSpan.FromMilliseconds(GetDoubleClickTime());
    }
}
