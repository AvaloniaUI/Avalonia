using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Logging;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Rendering;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using static Avalonia.Win32.DxgiSwapchain.DirectXUnmanagedMethods;
using MicroCom.Runtime;

namespace Avalonia.Win32.DxgiSwapchain
{
#pragma warning disable CA1416 // This should only be reachable on Windows
#nullable enable
    public unsafe class DxgiConnection : IRenderTimer
    {
        public const uint ENUM_CURRENT_SETTINGS = unchecked((uint)(-1));

        public bool RunsInBackground => true;

        public event Action<TimeSpan>? Tick;

        private AngleWin32EglDisplay _angle;
        private EglPlatformOpenGlInterface _gl;
        private object _syncLock;

        private IDXGIOutput? _output = null;

        private Stopwatch? _stopwatch = null;

        public DxgiConnection(EglPlatformOpenGlInterface gl, object syncLock)
        {

            _syncLock = syncLock;
            _angle = (AngleWin32EglDisplay)gl.Display;
            _gl = gl;
        }

        public EglPlatformOpenGlInterface Egl => _gl;

        public static void TryCreateAndRegister(EglPlatformOpenGlInterface angle)
        {
            try
            {
                TryCreateAndRegisterCore(angle);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, nameof(DxgiSwapchain))
                    ?.Log(null, "Unable to establish Dxgi: {0}", ex);
            }
        }

        private unsafe void RunLoop()
        {
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                GetBestOutputToVWaitOn();
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, nameof(DxgiSwapchain))
                                    ?.Log(this, $"Failed to wait for vblank, Exception: {ex.Message}, HRESULT = {ex.HResult}");
            }

            while (true)
            {
                try
                {
                    lock (_syncLock)
                    {
                        if (_output is not null)
                        {
                            try
                            {
                                _output.WaitForVBlank();
                            }
                            catch (Exception ex)
                            {
                                Logger.TryGet(LogEventLevel.Error, nameof(DxgiSwapchain))
                                    ?.Log(this, $"Failed to wait for vblank, Exception: {ex.Message}, HRESULT = {ex.HResult}");
                                _output.Dispose();
                                _output = null;
                                GetBestOutputToVWaitOn();
                            }
                        }
                        else
                        {
                            // well since that obviously didn't work, then let's use the lowest-common-denominator instead 
                            // for reference, this has never happened on my machine,
                            // but theoretically someone could have a weirder setup out there 
                            DwmFlush();
                        }
                        Tick?.Invoke(_stopwatch.Elapsed);
                    }
                }
                catch (Exception ex)
                {
                    Logger.TryGet(LogEventLevel.Error, nameof(DxgiSwapchain))
                                    ?.Log(this, $"Failed to wait for vblank, Exception: {ex.Message}, HRESULT = {ex.HResult}");
                }
            }
        }

        // Note: Defining best as display with highest refresh rate on 
        private void GetBestOutputToVWaitOn()
        {
            double highestRefreshRate = 0.0d;

            // IDXGIFactory Guid: [Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369")]
            Guid factoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory));
            CreateDXGIFactory(ref factoryGuid, out var factPointer);

            using var fact = MicroComRuntime.CreateProxyFor<IDXGIFactory>(factPointer, true);

            void* adapterPointer = null;

            ushort adapterIndex = 0;

            // this looks odd, but that's just how one enumerates adapters in DXGI 
            while (fact.EnumAdapters(adapterIndex, &adapterPointer) == 0)
            {
                using var adapter = MicroComRuntime.CreateProxyFor<IDXGIAdapter>(adapterPointer, true);
                void* outputPointer = null;
                ushort outputIndex = 0;
                while (adapter.EnumOutputs(outputIndex, &outputPointer) == 0)
                {
                    using var output = MicroComRuntime.CreateProxyFor<IDXGIOutput>(outputPointer, true);
                    DXGI_OUTPUT_DESC outputDesc = output.Desc;


                    // this handle need not closing, by the way. 
                    HANDLE monitorH = outputDesc.Monitor;
                    MONITORINFOEXW monInfo = default;
                    // by setting cbSize we tell Windows to fully populate the extended info 

                    monInfo.Base.cbSize = sizeof(MONITORINFOEXW);
                    GetMonitorInfoW(monitorH, (IntPtr)(&monInfo));

                    DEVMODEW devMode = default;
                    EnumDisplaySettingsW(outputDesc.DeviceName, ENUM_CURRENT_SETTINGS, &devMode);

                    if (highestRefreshRate < devMode.dmDisplayFrequency)
                    {
                        // ooh I like this output! 
                        if (_output is not null)
                        {
                            _output.Dispose();
                            _output = null;
                        }
                        _output = MicroComRuntime.CloneReference(output);
                        highestRefreshRate = devMode.dmDisplayFrequency;
                    }
                    // and then increment index to move onto the next monitor 
                    outputIndex++;
                }
                // and then increment index to move onto the next display adapater
                adapterIndex++;
            }

        }

        // Used the windows composition as a blueprint for this startup/creation 
        static private bool TryCreateAndRegisterCore(EglPlatformOpenGlInterface gl)
        {
            var tcs = new TaskCompletionSource<bool>();
            var pumpLock = new object();
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    DxgiConnection connection;

                    connection = new DxgiConnection(gl, pumpLock);

                    AvaloniaLocator.CurrentMutable.BindToSelf(connection);
                    AvaloniaLocator.CurrentMutable.Bind<IRenderTimer>().ToConstant(connection);
                    tcs.SetResult(true);
                    connection.RunLoop();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            thread.IsBackground = true;
            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            // block until 
            return tcs.Task.Result;
        }
    }
#nullable restore
#pragma warning restore CA1416 // Validate platform compatibility
}
