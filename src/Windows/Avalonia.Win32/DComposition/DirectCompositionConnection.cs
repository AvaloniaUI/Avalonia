using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.MicroCom;
using Avalonia.OpenGL.Egl;
using Avalonia.Rendering;
using Avalonia.Win32.Interop;
using Avalonia.Win32.WinRT;
using Avalonia.Win32.WinRT.Composition;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositionConnection : IRenderTimer, IWindowsSurfaceFactory
{
    private static readonly Guid IID_IDCompositionDesktopDevice = Guid.Parse("5f4633fe-1e08-4cb8-8c75-ce24333f5602");

    public event Action<TimeSpan>? Tick;
    public bool RunsInBackground => true;

    private readonly DirectCompositionShared _shared;

    public DirectCompositionConnection(DirectCompositionShared shared)
    {
        _shared = shared;
    }
    
    private static bool TryCreateAndRegisterCore()
    {
        var tcs = new TaskCompletionSource<bool>();
        var th = new Thread(() =>
        {
            DirectCompositionConnection connect;
            try
            {
                var result = NativeMethods.DCompositionCreateDevice2(default, IID_IDCompositionDesktopDevice, out var cDevice);
                if (result != UnmanagedMethods.HRESULT.S_OK)
                {
                    throw new Win32Exception((int)result);
                }

                using (var device = MicroComRuntime.CreateProxyFor<IDCompositionDesktopDevice>(cDevice, false))
                {
                    var shared = new DirectCompositionShared(device);
                    connect = new DirectCompositionConnection(shared);
                }

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
        }) { IsBackground = true, Name = "DwmRenderTimerLoop" };
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
        return tcs.Task.Result;
    }

    private void RunLoop()
    {
        var cts = new CancellationTokenSource();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            cts.Cancel();

        var _stopwatch = Stopwatch.StartNew();
        var device = _shared.Device.CloneReference();
        
        while (!cts.IsCancellationRequested)
        {
            try
            {
                device.WaitForCommitCompletion();
                Tick?.Invoke(_stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Win32Platform)
                    ?.Log(this, $"Failed to wait for vblank, Exception: {ex.Message}, HRESULT = {ex.HResult}");
            }
        }
        
        device?.Dispose();
    }

    public static bool IsSupported()
    {
        return Win32Platform.WindowsVersion >= PlatformConstants.Windows8_1;
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
                Logger.TryGet(LogEventLevel.Error, LogArea.Win32Platform)
                    ?.Log(null, "Unable to initialize WinUI compositor: {0}", e);
            }
        }
        else
        {
            var osVersionNotice =
                $"Windows {PlatformConstants.Windows8_1} is required. Your machine has Windows {Win32Platform.WindowsVersion} installed.";

            Logger.TryGet(LogEventLevel.Warning, LogArea.Win32Platform)?.Log(null,
                $"Unable to initialize WinUI compositor: {osVersionNotice}");
        }

        return false;
    }

    public bool RequiresNoRedirectionBitmap => true;
    public object CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info) => new DirectCompositedWindowSurface(_shared, info);
}
