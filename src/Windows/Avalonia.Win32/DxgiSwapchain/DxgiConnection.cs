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

        public HANDLE AwaitableHandle = HANDLE.NULL;

        private IDXGIOutput* _output = null;

        private Stopwatch? _stopwatch = null;
        private bool _skip = false;

        public DxgiConnection(EglPlatformOpenGlInterface gl, object syncLock)
        {

            _syncLock = syncLock;
            _angle = (AngleWin32EglDisplay)gl.Display;
            _gl = gl;
        }

        public EglPlatformOpenGlInterface Egl
        {
            get => _gl;
        }

        public static void TryCreateAndRegister(EglPlatformOpenGlInterface angle)
        {
            try
            {
                TryCreateAndRegisterCore(angle);
            }
            catch (Exception ex)
            {
                Logger.TryGet(LogEventLevel.Error, "DxgiSwapchain")
                    ?.Log(null, "Unable to establish Dxgi: {0}", ex);
            }
        }

        public void TickNow()
        {
            if (_stopwatch is not null)
            {
                lock (_syncLock)
                {
                    // note: No need to retest _stopwatch as we never set it back to null here 
                    Tick?.Invoke(_stopwatch.Elapsed);
                    _skip = true;
                }
            }
        }

        private unsafe void RunLoop()
        {
            _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            GetBestOutputToVWaitOn();

            while (true)
            {
                lock (_syncLock)
                {
                    if (_output is not null)
                    {
                        HRESULT res = _output->WaitForVBlank();
                        if (res.FAILED)
                        {
                            // be sad about it 
                            var w32ex = new Win32Exception((int)res);
                            Logger.TryGet(LogEventLevel.Error, nameof(DxgiSwapchain))
                                ?.Log(this, $"Failed to wait for vblank, Exception: {w32ex.Message}, HRESULT = {w32ex.HResult}");

                            _output->Release();
                            _output = null;
                            GetBestOutputToVWaitOn();
                        }
                    }
                    else
                    {
                        // well, then let's use the lowest-common-denominator instead 
                        DwmFlush();
                    }
                    if (_skip)
                    {
                        _skip = false;
                        continue;
                    }
                    Tick?.Invoke(_stopwatch.Elapsed);
                }
            }
        }

        private void GetBestOutputToVWaitOn()
        {
            double highestRefreshRate = 0.0d;
            HRESULT retval = default;
            IDXGIFactory* fact = null;
            // IDXGIFactory Guid: [Guid("7B7166EC-21C7-44AE-B21A-C9AE321AE369")]
            Guid factoryGuid = Guids.IDXGIFactoryGuid;
            retval = CreateDXGIFactory(&factoryGuid, (void**)&fact);
            if (retval.FAILED)
            {
                // To be clear, if this fails then it means we've tried to use dxgi methods on a system that does not support any of them 
                // I fully expect that if we hit this, then the application is done for. I don't think ANGLE would work either, frankly. 
                throw new Win32Exception((int)retval);
            }

            IDXGIAdapter* adapter = null;

            uint adapterIndex = 0;
            try
            {
                while (fact->EnumAdapters(adapterIndex, &adapter) == 0)
                {
                    IDXGIOutput* output = null;
                    uint outputIndex = 0;
                    while (adapter->EnumOutputs(outputIndex, &output) == 0)
                    {
                        DXGI_OUTPUT_DESC outputDesc = default;
                        retval = output->GetDesc(&outputDesc);

                        if (retval.SUCCEEDED)
                        {
                            HANDLE monitorH = outputDesc.Monitor;
                            MONITORINFOEXW monInfo = default;
                            // by setting cbSize we tell Windows to fully populate the extended info 

                            monInfo.Base.cbSize = sizeof(MONITORINFOEXW);
                            GetMonitorInfoW(monitorH, (IntPtr)(&monInfo));

                            DEVMODEW devMode = default;
                            EnumDisplaySettingsW(outputDesc.DeviceName, ENUM_CURRENT_SETTINGS, &devMode);

                            //Trace.WriteLine($"Adapter[{adapterIndex}] Output[{outputIndex}]: " +
                            //    $"{new string((char*)outputDesc.DeviceName)}, {new string((char*)monInfo.szDevice)}, " +
                            //    $"devModeHz: {devMode.dmDisplayFrequency} Hz");

                            if (highestRefreshRate < devMode.dmDisplayFrequency)
                            {
                                // ooh I like this output! 
                                _output = output;
                                highestRefreshRate = devMode.dmDisplayFrequency;
                            }
                        }

                        // clean up output, but only if it's not the one we selected 
                        if (output != _output)
                        {
                            output->Release();
                            output = null;
                        }
                        // and then increment index
                        outputIndex++;
                    }
                    // clean up adapter 
                    adapter->Release();
                    adapter = null;
                    // and then increment index 
                    adapterIndex++;
                }
            }
            finally
            {
                if (fact is not null)
                {
                    fact->Release();
                    fact = null;
                }
            }
        }


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

        #region unsafe native methods

        [DllImport("dwmapi", ExactSpelling = true)]
        public static extern HRESULT DwmFlush();

        #endregion
    }
#nullable restore
#pragma warning restore CA1416 // Validate platform compatibility
}
