using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using D3DDevice = SharpDX.Direct3D11.Device;
using DxgiFactory1 = SharpDX.DXGI.Factory1;
namespace GpuInterop.VulkanDemo;

public class D3DMemoryHelper
{
    public static D3DDevice CreateDeviceByLuid(Span<byte> luid)
    {
        var factory = new DxgiFactory1();
        var longLuid = MemoryMarshal.Cast<byte, long>(luid)[0];
        for (var c = 0; c < factory.GetAdapterCount1(); c++)
        {
            using var adapter = factory.GetAdapter1(0);
            if (adapter.Description1.Luid != longLuid)
                continue;

            return new D3DDevice(adapter, DeviceCreationFlags.None,
                new[]
                {
                    FeatureLevel.Level_12_1, FeatureLevel.Level_12_0, FeatureLevel.Level_11_1,
                    FeatureLevel.Level_11_0, FeatureLevel.Level_10_0, FeatureLevel.Level_9_3,
                    FeatureLevel.Level_9_2, FeatureLevel.Level_9_1,
                });
        }

        throw new ArgumentException("Device with the corresponding LUID not found");
    }

    public static Texture2D CreateMemoryHandle(D3DDevice device, PixelSize size, Silk.NET.Vulkan.Format format)
    {
        if (format != Silk.NET.Vulkan.Format.R8G8B8A8Unorm)
            throw new ArgumentException("Not supported format");
        return new Texture2D(device,
            new Texture2DDescription
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = size.Width,
                Height = size.Height,
                ArraySize = 1,
                MipLevels = 1,
                SampleDescription = new SampleDescription { Count = 1, Quality = 0 },
                CpuAccessFlags = default,
                OptionFlags = ResourceOptionFlags.SharedKeyedmutex|ResourceOptionFlags.SharedNthandle,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource
            });
    }
}
