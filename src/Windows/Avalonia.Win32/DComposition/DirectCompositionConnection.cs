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

internal class DirectCompositionConnection : IRenderTimer, ICompositorConnection
{
    public event Action<TimeSpan>? Tick;
    public bool RunsInBackground => true;

    private readonly DirectCompositionShared _shared;

    public DirectCompositionConnection()
    {
        _shared = new DirectCompositionShared();
    }
    
    private static bool TryCreateAndRegisterCore()
    {
        var tcs = new TaskCompletionSource<bool>();
        var th = new Thread(() =>
        {
            DirectCompositionConnection connect;
            try
            {
                connect = new DirectCompositionConnection();
                
                AvaloniaLocator.CurrentMutable.Bind<ICompositorConnection>().ToConstant(connect);
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
        IDCompositionDevice? device = null;
        
        while (!cts.IsCancellationRequested)
        {
            try
            {
                if (device is null)
                {
                    // We don't use EventWaitHandle, as we can't block thread and we need to raise Tick before any window created.
                    // We don't have any locks for _shared.Device, as we are inside of the loop, and it's low impact.
                    Thread.Sleep(1);
                    device = _shared.Device?.CloneReference();
                }
                else
                {
                    device.WaitForCommitCompletion();
                }

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
        return Win32Platform.WindowsVersion >= PlatformConstants.Windows8;
    }

    public static bool TryCreateAndRegister()
    {
        if (Win32Platform.WindowsVersion >= PlatformConstants.Windows8)
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
                $"Windows {PlatformConstants.Windows8} is required. Your machine has Windows {Win32Platform.WindowsVersion} installed.";

            Logger.TryGet(LogEventLevel.Warning, "WinUIComposition")?.Log(null,
                $"Unable to initialize WinUI compositor: {osVersionNotice}");
        }

        return false;
    }

    public Win32CompositionMode CompositionMode => Win32CompositionMode.DirectComposition;
    public bool TransparencySupported => true;
    public bool AcrylicSupported { get; }
    public bool MicaSupported { get; }
    public object CreateSurface(EglGlPlatformSurface.IEglWindowGlPlatformSurfaceInfo info) => new DirectCompositedWindowSurface(_shared, info);
}
