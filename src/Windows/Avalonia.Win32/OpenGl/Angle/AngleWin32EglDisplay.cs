using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Angle;
using Avalonia.OpenGL.Egl;
using Avalonia.Win32.DirectX;
using MicroCom.Runtime;
using static Avalonia.OpenGL.Egl.EglConsts;
// ReSharper disable SimplifyLinqExpressionUseMinByAndMaxBy

namespace Avalonia.Win32.OpenGl.Angle
{
    internal class AngleWin32EglDisplay : EglDisplay
    {
        protected override bool DisplayLockIsSharedWithContexts => true;

        public static AngleWin32EglDisplay CreateD3D9Display(EglInterface egl)
        {
            var display = egl.GetPlatformDisplayExt(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D9_ANGLE, EGL_NONE });
            
            return new AngleWin32EglDisplay(display, new EglDisplayOptions()
            {
                Egl = egl,
                ContextLossIsDisplayLoss = true,
                GlVersions = AvaloniaLocator.Current.GetService<AngleOptions>()?.GlProfiles
            }, AngleOptions.PlatformApi.DirectX9);
        }
        
        public static AngleWin32EglDisplay CreateSharedD3D11Display(EglInterface egl)
        {
            var display = egl.GetPlatformDisplayExt(EGL_PLATFORM_ANGLE_ANGLE, IntPtr.Zero,
                new[] { EGL_PLATFORM_ANGLE_TYPE_ANGLE, EGL_PLATFORM_ANGLE_TYPE_D3D11_ANGLE, EGL_NONE });
            
            return new AngleWin32EglDisplay(display, new EglDisplayOptions()
            {
                Egl = egl,
                ContextLossIsDisplayLoss = true,
                GlVersions = AvaloniaLocator.Current.GetService<AngleOptions>()?.GlProfiles
            }, AngleOptions.PlatformApi.DirectX11);
        }

        public static unsafe AngleWin32EglDisplay CreateD3D11Display(Win32AngleEglInterface egl,
            bool preferDiscreteAdapter = false)
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

                void* pAdapter = null;
                if (preferDiscreteAdapter)
                {
                    ushort adapterIndex = 0;
                    var adapters = new List<(IDXGIAdapter1 adapter, string name)>();
                    while (factory.EnumAdapters1(adapterIndex, &pAdapter) == 0)
                    {
                        var adapter = MicroComRuntime.CreateProxyFor<IDXGIAdapter1>(pAdapter, true);
                        var desc = adapter.Desc1;
                        var name = Marshal.PtrToStringUni(new IntPtr(desc.Description))!.ToLowerInvariant();
                        adapters.Add((adapter, name));
                        adapterIndex++;
                    }

                    if (adapters.Count == 0)
                        throw new OpenGlException("No adapters found");
                    chosenAdapter = adapters
                        .OrderByDescending(x => x.name.Contains("nvidia") ? 2 : x.name.Contains("amd") ? 1 : 0)
                        .First().adapter.CloneReference();
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


                var rv = new AngleWin32EglDisplay(display,
                    new EglDisplayOptions
                    {
                        DisposeCallback = Cleanup,
                        Egl = egl,
                        ContextLossIsDisplayLoss = true,
                        DeviceLostCheckCallback = () => d3dDevice.DeviceRemovedReason != 0,
                        GlVersions = AvaloniaLocator.Current.GetService<AngleOptions>()?.GlProfiles
                    }, AngleOptions.PlatformApi.DirectX11);
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

        private AngleWin32EglDisplay(IntPtr display, EglDisplayOptions options, AngleOptions.PlatformApi platformApi) : base(display, options)
        {
            PlatformApi = platformApi;
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

        public EglSurface WrapDirect3D11Texture( IntPtr handle)
        {
            if (PlatformApi != AngleOptions.PlatformApi.DirectX11)
                throw new InvalidOperationException("Current platform API is " + PlatformApi);
            return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, new[] { EGL_NONE, EGL_NONE });            
        }

        public EglSurface WrapDirect3D11Texture(IntPtr handle, int offsetX, int offsetY, int width, int height)
        {
            if (PlatformApi != AngleOptions.PlatformApi.DirectX11)
                throw new InvalidOperationException("Current platform API is " + PlatformApi);
            return CreatePBufferFromClientBuffer(EGL_D3D_TEXTURE_ANGLE, handle, new[] { EGL_WIDTH, width, EGL_HEIGHT, height, EGL_FLEXIBLE_SURFACE_COMPATIBILITY_SUPPORTED_ANGLE, EGL_TRUE, EGL_TEXTURE_OFFSET_X_ANGLE, offsetX, EGL_TEXTURE_OFFSET_Y_ANGLE, offsetY, EGL_NONE });
        }
    }
}
