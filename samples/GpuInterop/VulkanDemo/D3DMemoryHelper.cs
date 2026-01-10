using System;
using Avalonia;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using VulkanFormat = Silk.NET.Vulkan.Format;
using static Silk.NET.Core.Native.SilkMarshal;

namespace GpuInterop.VulkanDemo;

public class D3DMemoryHelper
{
    private const int DxgiErrorNotFound = unchecked((int)0x887A0002);

    public static unsafe ComPtr<ID3D11Device> CreateDeviceByLuid(Luid luid)
    {
        using var dxgi = new DXGI(DXGI.CreateDefaultContext(["DXGI.dll"]));
        using var d3d11 = new D3D11(D3D11.CreateDefaultContext(["d3d11.dll"]));
        using var factory = dxgi.CreateDXGIFactory1<IDXGIFactory1>();
        using var adapter = GetAdapterByLuid(factory, luid);

        const int featureLevelCount = 8;
        var featureLevels = stackalloc D3DFeatureLevel[featureLevelCount]
        {
            D3DFeatureLevel.Level121,
            D3DFeatureLevel.Level120,
            D3DFeatureLevel.Level111,
            D3DFeatureLevel.Level110,
            D3DFeatureLevel.Level100,
            D3DFeatureLevel.Level93,
            D3DFeatureLevel.Level92,
            D3DFeatureLevel.Level91
        };

        ComPtr<ID3D11Device> device = default;
        ComPtr<ID3D11DeviceContext> context = default;
        D3DFeatureLevel actualFeatureLevel;
        ThrowHResult(d3d11.CreateDevice(
            adapter,
            D3DDriverType.Unknown,
            IntPtr.Zero,
            0u,
            featureLevels,
            featureLevelCount,
            D3D11.SdkVersion,
            device.GetAddressOf(),
            &actualFeatureLevel,
            context.GetAddressOf()));

        return device;
    }

    private static unsafe ComPtr<IDXGIAdapter> GetAdapterByLuid(ComPtr<IDXGIFactory1> factory, Luid luid)
    {
        var index = 0u;
        ComPtr<IDXGIAdapter> adapter = default;

        while (factory.EnumAdapters(index, adapter.GetAddressOf()) != DxgiErrorNotFound)
        {
            AdapterDesc adapterDesc;
            if (adapter.GetDesc(&adapterDesc) == 0 & AreLuidsEqual(adapterDesc.AdapterLuid, luid))
                return adapter;

            adapter.Dispose();
            ++index;
        }

        throw new ArgumentException("Device with the corresponding LUID not found");
    }

    public static unsafe ComPtr<ID3D11Texture2D> CreateMemoryHandle(ComPtr<ID3D11Device> device, PixelSize size, VulkanFormat format)
    {
        if (format != VulkanFormat.R8G8B8A8Unorm)
            throw new ArgumentException("Not supported format");

        ComPtr<ID3D11Texture2D> texture = default;
        var textureDesc = new Texture2DDesc
        {
            Format = Format.FormatR8G8B8A8Unorm,
            Width = (uint)size.Width,
            Height = (uint)size.Height,
            ArraySize = 1,
            MipLevels = 1,
            SampleDesc = new SampleDesc(1, 0),
            Usage = Usage.Default,
            BindFlags = (uint)(BindFlag.RenderTarget | BindFlag.ShaderResource),
            CPUAccessFlags = 0,
            MiscFlags = (uint)(ResourceMiscFlag.SharedKeyedmutex | ResourceMiscFlag.SharedNthandle)
        };
        ThrowHResult(device.CreateTexture2D(&textureDesc, (SubresourceData*)null, texture.GetAddressOf()));

        return texture;
    }

    private static bool AreLuidsEqual(Luid x, Luid y)
        => x.Low == y.Low && x.High == y.High;
}
