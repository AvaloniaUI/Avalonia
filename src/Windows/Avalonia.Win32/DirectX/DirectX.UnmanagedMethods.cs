using System;
using System.Runtime.InteropServices;
using Avalonia.MicroCom;

namespace Avalonia.Win32.DirectX
{
    internal enum D3D_DRIVER_TYPE
    {
        Unknown,
        Hardware,
        Reference,
        Null,
        Software,
        Warp,
    }

    enum D3D_FEATURE_LEVEL
    {
        D3D_FEATURE_LEVEL_9_1 = 0x00009100,
        D3D_FEATURE_LEVEL_9_2 = 0x00009200,
        D3D_FEATURE_LEVEL_9_3 = 0x00009300,
        D3D_FEATURE_LEVEL_10_0 = 0x0000A000,
        D3D_FEATURE_LEVEL_10_1 = 0x0000A100,
        D3D_FEATURE_LEVEL_11_0 = 0x0000B000,
        D3D_FEATURE_LEVEL_11_1 = 0x0000B100,
        D3D_FEATURE_LEVEL_12_0 = 0x0000C000,
        D3D_FEATURE_LEVEL_12_1 = 0x0000C100
    }

    internal static class DirectXUnmanagedMethods
    {
        [DllImport("dxgi.dll", PreserveSig = false)]
        static extern IntPtr CreateDXGIFactory1(ref Guid guid);

        [DllImport("d3d11.dll", PreserveSig = false)]
        static extern void D3D11CreateDevice(
            IntPtr pAdapter,
            D3D_DRIVER_TYPE DriverType,
            IntPtr Software,
            uint Flags,
            D3D_FEATURE_LEVEL[] pFeatureLevels,
            uint FeatureLevels,
            uint SDKVersion,
            out IntPtr ppDevice,
            out D3D_FEATURE_LEVEL pFeatureLevel,
            out IntPtr ppImmediateContext
        );
        
        public static IDXGIFactory1 CreateDxgiFactory()
        {
            var guid = MicroComRuntime.GetGuidFor(typeof(IDXGIFactory1));
            return MicroComRuntime.CreateProxyFor<IDXGIFactory1>(CreateDXGIFactory1(ref guid), true);
        }

        public static ID3D11Device CreateDevice(IDXGIAdapter adapter, D3D_FEATURE_LEVEL[] levels)
        {
            D3D11CreateDevice(MicroComRuntime.GetNativeIntPtr(adapter), D3D_DRIVER_TYPE.Unknown,
                IntPtr.Zero, 0, levels, (uint)levels.Length, 7, out var pDevice, out var featureLevel, out var context);
            Marshal.Release(context);
            return MicroComRuntime.CreateProxyFor<ID3D11Device>(pDevice, true);
        }
    }
}
