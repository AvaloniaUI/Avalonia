using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.MicroCom;
using Avalonia.OpenGL.Egl;
using Avalonia.Rendering;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;
using Avalonia.Win32.OpenGl.Angle;
using MicroCom.Runtime;

namespace Avalonia.Win32.WinRT.Composition;

internal class WinUiCompositorConnection : IRenderTimer
{
    private readonly WinUiCompositionShared _shared;
    public event Action<TimeSpan> Tick;
    public bool RunsInBackground => true;
    
    public unsafe WinUiCompositorConnection()
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

    static bool TryCreateAndRegisterCore()
    {
        var tcs = new TaskCompletionSource<bool>();
        var pumpLock = new object();
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
                AvaloniaLocator.CurrentMutable.BindToSelf(connect);
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

    class RunLoopHandler : CallbackBase, IAsyncActionCompletedHandler 
    {
        private readonly WinUiCompositorConnection _parent;
        private Stopwatch _st = Stopwatch.StartNew();

        public RunLoopHandler(WinUiCompositorConnection parent)
        {
            _parent = parent;
        }
        
        public void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        { 
            _parent.Tick?.Invoke(_st.Elapsed);
            using var act = _parent._shared.Compositor5.RequestCommitAsync();
            act.SetCompleted(this);
        }
    }

    private void RunLoop()
    {
        var cts = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            cts.Cancel();

        lock (_shared.SyncRoot)
            using (var act = _shared.Compositor5.RequestCommitAsync())
                act.SetCompleted(new RunLoopHandler(this));

        while (!cts.IsCancellationRequested)
        {
            UnmanagedMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0);
            lock (_shared.SyncRoot)
                UnmanagedMethods.DispatchMessage(ref msg);
        }
    }

    public static void TryCreateAndRegister()
    {
        const int majorRequired = 10;
        const int buildRequired = 17134;

        var majorInstalled = Win32Platform.WindowsVersion.Major;
        var buildInstalled = Win32Platform.WindowsVersion.Build;

        if (majorInstalled >= majorRequired &&
            buildInstalled >= buildRequired)
        {
            try
            {
                TryCreateAndRegisterCore();
                return;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "WinUIComposition")
                    ?.Log(null, "Unable to initialize WinUI compositor: {0}", e);

            }
        }

        var osVersionNotice =
            $"Windows {majorRequired} Build {buildRequired} is required. Your machine has Windows {majorInstalled} Build {buildInstalled} installed.";

        Logger.TryGet(LogEventLevel.Warning, "WinUIComposition")?.Log(null,
            $"Unable to initialize WinUI compositor: {osVersionNotice}");
    }

    public WinUiCompositedWindowSurface CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info) => new(_shared, info);
}
