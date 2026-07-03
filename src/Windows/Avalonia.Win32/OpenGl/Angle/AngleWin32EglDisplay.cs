using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Logging;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Win32.DirectX;
using MicroCom.Runtime;
using static Avalonia.OpenGL.Egl.EglConsts;
// ReSharper disable SimplifyLinqExpressionUseMinByAndMaxBy

namespace Avalonia.Win32.OpenGl.Angle
{
    internal class AngleWin32EglDisplay : EglDisplay
    {
        private readonly bool _flexibleSurfaceSupported;

        protected override bool DisplayLockIsSharedWithContexts => true;

        static EglDisplayOptions GetDisplayOptions(EglInterface egl,
            Func<bool>? deviceLostCheckCallback = null,
            Action? disposeCallback = null) => new()
        {
            Egl = egl,
            ContextLossIsDisplayLoss = true,
            GlVersions = AvaloniaLocator.Current.GetService<AngleOptions>()?.GlProfiles
                .Where(x => x.Type == GlProfileType.OpenGLES),
            DeviceLostCheckCallback = deviceLostCheckCallback,
            DisposeCallback = disposeCallback
        };
        
        public static AngleWin32EglDisplay CreateD3D9Display(EglInterface egl)
        {
            var display = egl.GetPlatformDisplayExt(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE, EGL_NONE });
            
            return new AngleWin32EglDisplay(display, egl, GetDisplayOptions(egl), AngleOptions.PlatformApi.DirectX9);
        }
        
        public static AngleWin32EglDisplay CreateSharedD3D11Display(EglInterface egl)
        {
            var display = egl.GetPlatformDisplayExt(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE, EGL_NONE });
            
            return new AngleWin32EglDisplay(display, egl, GetDisplayOptions(egl), AngleOptions.PlatformApi.DirectX11);
        }

        public static unsafe AngleWin32EglDisplay CreateD3D11Display(Win32AngleEglInterface egl)
        {
            var featureLevels = new[]
            {
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_1, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_11_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_1, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_10_0,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_3, D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_2,
                D3D_FEATURE_LEVEL.D3D_FEATURE_LEVEL_9_1
            };

            var dxgiFactoryGuid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory1));
            DirectXUnmanagedMethods.CreateDXGIFactory1(ref dxgiFactoryGuid, out var pDxgiFactory);
            IDXGIAdapter1? chosenAdapter = null;
            if (pDxgiFactory != null)
            {
                using var factory = MicroComRuntime.CreateProxyFor<IDXGIFactory1>(pDxgiFactory, true);

                var selectionCallback = AvaloniaLocator.Current.GetService<Win32PlatformOptions>()
                    ?.GraphicsAdapterSelectionCallback;
                
                void* pAdapter = null;
                
                var applyArmAdrenoBlacklist = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
                
                // As for now, we only need to redefine default adapter only on ARM64 just in case of Adreno GPU.
                var redefineDefaultAdapter =
                    selectionCallback != null
                    || applyArmAdrenoBlacklist;
                
                if (redefineDefaultAdapter)
                {
                    ushort adapterIndex = 0;
                    var adapters = new List<(IDXGIAdapter1 adapter, PlatformGraphicsDeviceAdapterDescription desc)>();
                    while (factory.EnumAdapters1(adapterIndex, &pAdapter) == 0)
                    {
                        var adapter = MicroComRuntime.CreateProxyFor<IDXGIAdapter1>(pAdapter, true);
                        var desc = adapter.Desc1;
                        var name = Marshal.PtrToStringUni(new IntPtr(desc.Description))?.ToLowerInvariant();
                        var luid = BitConverter.GetBytes(desc.AdapterLuid);
                        adapters.Add((adapter, new() { Description = name, DeviceLuid = luid }));
                        adapterIndex++;
                    }

                    if (adapters.Count == 0)
                        throw new OpenGlException("No adapters found");

                    // The Adreno blacklist (see #10405) now only moves the *default* selection away
                    // from Adreno GPUs - it no longer hides adapters from the selection callback, so
                    // an application can deliberately opt back into hardware acceleration.
                    var chosenAdapterIndex = 0;
                    if (selectionCallback != null)
                    {
                        chosenAdapterIndex = selectionCallback(adapters.Select(a => a.desc).ToArray());
                    }
                    else if (applyArmAdrenoBlacklist && adapters.Count > 1)
                    {
                        var firstNonAdreno = adapters.FindIndex(a => a.desc.Description?.Contains("adreno") != true);
                        if (firstNonAdreno > 0)
                        {
                            chosenAdapterIndex = firstNonAdreno;
                            Logger.TryGet(LogEventLevel.Warning, "OpenGL")?.Log(null,
                                "ARM64 Adreno GPU detected; the Adreno rendering blocklist is forcing a " +
                                "fallback to '{FallbackAdapter}' (typically the software Microsoft Basic " +
                                "Render Driver), which can significantly increase CPU usage. Set " +
                                "Win32PlatformOptions.GraphicsAdapterSelectionCallback to choose an adapter " +
                                "explicitly, or switch to Win32RenderingMode.Vulkan.",
                                adapters[firstNonAdreno].desc.Description);
                        }
                    }

                    chosenAdapter = adapters[chosenAdapterIndex].adapter.CloneReference();

                    foreach (var a in adapters)
                        a.adapter.Dispose();
                }
                else
                {
                    if (factory.EnumAdapters1(0, &pAdapter) != 0)
                        throw new OpenGlException("No adapters found");
                    chosenAdapter = MicroComRuntime.CreateProxyFor<IDXGIAdapter1>(pAdapter, true);
                }
            }

            IntPtr pD3dDevice;
            using (chosenAdapter)
                DirectXUnmanagedMethods.D3D11CreateDevice(chosenAdapter?.GetNativeIntPtr() ?? IntPtr.Zero,
                    D3D_DRIVER_TYPE.D3D_DRIVER_TYPE_UNKNOWN,
                    IntPtr.Zero, 0, featureLevels, (uint)featureLevels.Length,
                    7, out pD3dDevice, out _, null);


            if (pD3dDevice == IntPtr.Zero)
                throw new Win32Exception("Unable to create D3D11 Device");

            var d3dDevice = MicroComRuntime.CreateProxyFor<ID3D11Device>(pD3dDevice, true);
            var angleDevice = IntPtr.Zero;
            var display = IntPtr.Zero;

            void Cleanup()
            {
                if (angleDevice != IntPtr.Zero)
                    egl.ReleaseDeviceANGLE(angleDevice);
                d3dDevice.Dispose();
            }

            bool success = false;
            try
            {
                angleDevice = egl.CreateDeviceANGLE(EGL_D3D11_DEVICE_ANGLE, pD3dDevice, null);
                if (angleDevice == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglCreateDeviceANGLE", egl);

                display = egl.GetPlatformDisplayExt(EGL_PLATFORM_DEVICE_EXT, angleDevice, null);
                if (display == IntPtr.Zero)
                    throw OpenGlException.GetFormattedException("eglGetPlatformDisplayEXT", egl);


                var rv = new AngleWin32EglDisplay(display, egl,
                    GetDisplayOptions(egl, () => d3dDevice.DeviceRemovedReason != 0, Cleanup),
                    AngleOptions.PlatformApi.DirectX11);
                success = true;
                return rv;
            }
            finally
            {
                if (!success)
                {
                    if (display != IntPtr.Zero)
                        egl.Terminate(display);
                    Cleanup();
                }
            }
        }

        private AngleWin32EglDisplay(IntPtr display, EglInterface egl, EglDisplayOptions options, AngleOptions.PlatformApi platformApi) : base(display, options)
        {
            PlatformApi = platformApi;
            var extensions = egl.QueryString(display, EGL_EXTENSIONS);
            _flexibleSurfaceSupported = extensions?.Contains("EGL_ANGLE_flexible_surface_compatibility") ?? false;
        }

        public AngleOptions.PlatformApi PlatformApi { get; }

        public IntPtr GetDirect3DDevice()
        {
            if (!EglInterface.QueryDisplayAttribExt(Handle, EglConsts.EGL_DEVICE_EXT, out var eglDevice))
                throw new OpenGlException("Unable to get EGL_DEVICE_EXT");
            if (!EglInterface.QueryDeviceAttribExt(eglDevice, PlatformApi == AngleOptions.PlatformApi.DirectX9 ? EGL_D3D9_DEVICE_ANGLE : EGL_D3D11_DEVICE_ANGLE, out var d3dDeviceHandle))
                throw new OpenGlException("Unable to get EGL_D3D9_DEVICE_ANGLE");
            return d3dDeviceHandle;
        }

        public unsafe EglSurface WrapDirect3D11Texture( IntPtr handle)
        {
            if (PlatformApi != AngleOptions.PlatformApi.DirectX11)
                ThrowInvalidPlatformApi();
            var attrs = stackalloc[] { EGL_NONE, EGL_NONE };
            return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, attrs);
        }

        public unsafe EglSurface WrapDirect3D11Texture(IntPtr handle, int offsetX, int offsetY, int width, int height)
        {
            if (PlatformApi != AngleOptions.PlatformApi.DirectX11)
                ThrowInvalidPlatformApi();
            var attrs = stackalloc[]
            {
                EGL_WIDTH, width, EGL_HEIGHT, height, EGL_TEXTURE_OFFSET_X_ANGLE, offsetX,
                EGL_TEXTURE_OFFSET_Y_ANGLE, offsetY,
                _flexibleSurfaceSupported ? EGL_FLEXIBLE_SURFACE_COMPATIBILITY_SUPPORTED_ANGLE : EGL_NONE, EGL_TRUE,
                EGL_NONE
            };
            return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, attrs);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ThrowInvalidPlatformApi()
        {
            throw new InvalidOperationException("Current platform API is " + PlatformApi);
        } 
    }
}
