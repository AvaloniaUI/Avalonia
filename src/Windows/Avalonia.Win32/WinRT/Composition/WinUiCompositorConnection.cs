using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.MicroCom;
using Avalonia.OpenGL.Egl;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositorConnection : IRenderTimer, Win32.IWindowsSurfaceFactory
{
    private readonly WinUiCompositionShared _shared;
    public event Action<TimeSpan>? Tick;
    public bool RunsInBackground => true;
    
    public WinUiCompositorConnection()
    {
        using var compositor = NativeWinRTMethods.CreateInstance<ICompositor>("Windows.UI.Composition.Compositor");
        /*
        var levels = new[] { D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1 };
        DirectXUnmanagedMethods.D3D11CreateDevice(IntPtr.Zero, D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_HARDWARE,
            IntPtr.Zero, 0, levels, (uint)levels.Length, 7, out var pD3dDevice, out var level, null);

        var d3dDevice = MicroComRuntime.CreateProxyFor<IUnknown>(pD3dDevice, true);

        var compositionDevice = compositor.QueryInterface<ICompositorInterop>().CreateGraphicsDevice(d3dDevice);
        var surf = compositionDevice.CreateDrawingSurface(new UnmanagedMethods.SIZE_F { X = 100, Y = 100 },
            DirectXPixelFormat.B8G8R8A8UIntNormalized, DirectXAlphaMode.Premultiplied);
        var surfInterop = surf.QueryInterface<ICompositionDrawingSurfaceInterop>();
        var IID_ID3D11Texture2D = Guid.Parse("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
        void* texture = null;
        surfInterop.BeginDraw(null, &IID_ID3D11Texture2D, &texture);
        */
        
        _shared = new WinUiCompositionShared(compositor);
    }

    private static bool TryCreateAndRegisterCore()
    {
        var tcs = new TaskCompletionSource<bool>();
        var th = new Thread(() =>
        {
            WinUiCompositorConnection connect;
            try
            {
                NativeWinRTMethods.CreateDispatcherQueueController(new NativeWinRTMethods.DispatcherQueueOptions
                {
                    apartmentType = NativeWinRTMethods.DISPATCHERQUEUE_THREAD_APARTMENTTYPE.DQTAT_COM_NONE,
                    dwSize = Marshal.SizeOf<NativeWinRTMethods.DispatcherQueueOptions>(),
                    threadType = NativeWinRTMethods.DISPATCHERQUEUE_THREAD_TYPE.DQTYPE_THREAD_CURRENT
                });
                connect = new WinUiCompositorConnection();
                AvaloniaLocator.CurrentMutable.Bind<IWindowsSurfaceFactory>().ToConstant(connect);
                AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(connect);
                tcs.SetResult(true);

            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }

            connect.RunLoop();
        })
        {
            IsBackground = true,
            Name = "DwmRenderTimerLoop"
        };
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
        return tcs.Task.Result;
    }

    private class RunLoopHandler : CallbackBase, IAsyncActionCompletedHandler
    {
        private readonly WinUiCompositorConnection _parent;
        private readonly Stopwatch _st = Stopwatch.StartNew();
        private TimeSpan? _commitDueAt;
        private IAsyncAction? _currentCommit;
        public RunLoopHandler(WinUiCompositorConnection parent)
        {
            _parent = parent;
        }

        public void Invoke(IAsyncAction? asyncInfo, AsyncStatus asyncStatus)
        { 
            if (_currentCommit == null ||
              _currentCommit.GetNativeIntPtr() != asyncInfo.GetNativeIntPtr())
                return;
            OnCommitCompleted();
        }
        
        private void OnCommitCompleted()
        {
            _currentCommit?.Dispose();
            _currentCommit = null;
            _parent.Tick?.Invoke(_st.Elapsed);
            ScheduleNextCommit();
        }

        private void ScheduleNextCommit()
        {
            lock (_parent._shared.SyncRoot)
            {
                _commitDueAt = _st.Elapsed + TimeSpan.FromSeconds(1);
                _currentCommit = _parent._shared.Compositor5.RequestCommitAsync();
                _currentCommit.SetCompleted(this);
            }
        }

        public void WatchDog()
        {
            // This is a workaround for a nasty WinUI composition API bug that prevents
            // RequestCommitAsync to ever complete after D3D device loss event with some systems
            // (A notable example is after pause/resume in Parallels Desktop)
            // We check if we haven't got a commit completion callback for a second
            // And forcefully trigger the next one, which makes the entire thing to unstuck
            
            if (_st.Elapsed > _commitDueAt && _currentCommit != null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this,
                    "windows::UI::Composition::ICompositor5.RequestCommitAsync timed out, force-triggering next tick");
                try
                {
                    _currentCommit?.GetResults();
                }
                catch (Exception e)
                {
                    Logger.TryGet(LogEventLevel.Error, LogArea.Visual)?.Log(this,
                        "ICompositor5::RequestCommitAsync failed: {HR}, {ERR}", e.HResult, e.ToString());
                }
                OnCommitCompleted();
            }
        }

        public void Start() => ScheduleNextCommit();
    }

    private void RunLoop()
    {
        var cts = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            cts.Cancel();

        var handler = new RunLoopHandler(this);
        handler.Start();

        using var dw = new SimpleWindow((hwnd, msg, w, l) =>
        {
            if (msg == (uint)UnmanagedMethods.WindowsMessage.WM_TIMER)
            {
                handler.WatchDog();
                UnmanagedMethods.SetTimer(hwnd, IntPtr.Zero, 1000, null);
            }
            return UnmanagedMethods.DefWindowProc(hwnd, msg, w, l);
        });
        UnmanagedMethods.SetTimer(dw.Handle, IntPtr.Zero, 1000, null);

        var result = 0;
        while (!cts.IsCancellationRequested
               && (result = UnmanagedMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0)) > 0)
        {
            lock (_shared.SyncRoot)
                UnmanagedMethods.DispatchMessage(ref msg);
        }

        if (result < 0)
        {
            Logger.TryGet(LogEventLevel.Error, "WinUIComposition")
                ?.Log(this, "Unmanaged error in {0}. Error Code: {1}", nameof(RunLoop), Marshal.GetLastWin32Error());
        }
    }

    public static bool IsSupported()
    {
        return Win32Platform.WindowsVersion >= WinUiCompositionShared.MinWinCompositionVersion;
    }
    
    public static bool TryCreateAndRegister()
    {
        if (IsSupported())
        {
            try
            {
                TryCreateAndRegisterCore();
                return true;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "WinUIComposition")
                    ?.Log(null, "Unable to initialize WinUI compositor: {0}", e);
            }
        }
        else
        {
            var osVersionNotice =
                $"Windows {WinUiCompositionShared.MinWinCompositionVersion} is required. Your machine has Windows {Win32Platform.WindowsVersion} installed.";

            Logger.TryGet(LogEventLevel.Warning, "WinUIComposition")?.Log(null,
                $"Unable to initialize WinUI compositor: {osVersionNotice}");
        }

        return false;
    }

    public bool RequiresNoRedirectionBitmap => true;
    public object CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info) => new WinUiCompositedWindowSurface(_shared, info);
}
