using System;
using System.Collections.Generic;
using Avalonia.UnitTests;
using Avalonia.Vulkan;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform;

public class VulkanPlatformGraphicsTests : ScopedTestBase
{
    [Fact]
    public void Disposing_A_Context_Disposes_The_Device_Created_For_It()
    {
        var factory = new FakeDeviceFactory();
        var graphics = new VulkanPlatformGraphics(factory, new VulkanPlatformSpecificOptions());

        graphics.CreateContext().Dispose();

        var device = Assert.Single(factory.CreatedDevices);
        Assert.True(device.IsDisposed);
    }

    [Fact]
    public void Disposing_A_Shared_Context_Keeps_The_Shared_Device()
    {
        var factory = new FakeDeviceFactory();
        var graphics = new VulkanPlatformGraphics(factory, new VulkanPlatformSpecificOptions());

        graphics.GetSharedContext().Dispose();

        Assert.False(factory.SharedDevice.IsDisposed);
    }

    private sealed class FakeDeviceFactory : VulkanPlatformGraphics.IVulkanDeviceFactory
    {
        public List<FakeDevice> CreatedDevices { get; } = new();
        public FakeDevice SharedDevice { get; } = new();
        public bool UsesShadedDevice => false;

        public IVulkanDevice GetSharedDevice(VulkanPlatformSpecificOptions platformOptions) => SharedDevice;

        public IVulkanDevice CreateDevice(VulkanPlatformSpecificOptions platformOptions)
        {
            var device = new FakeDevice();
            CreatedDevices.Add(device);
            return device;
        }
    }

    private sealed class FakeDevice : IVulkanDevice
    {
        public bool IsDisposed { get; private set; }
        public IntPtr Handle => new(1);
        public IntPtr PhysicalDeviceHandle => new(1);
        public IntPtr MainQueueHandle => new(1);
        public uint GraphicsQueueFamilyIndex => 0;
        public IVulkanInstance Instance { get; } = new FakeInstance();
        public bool IsLost => false;
        public IDisposable Lock() => new Unlocker();
        public IEnumerable<string> EnabledExtensions => Array.Empty<string>();
        public object? TryGetFeature(Type featureType) => null;
        public void Dispose() => IsDisposed = true;

        private sealed class Unlocker : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class FakeInstance : IVulkanInstance
    {
        // The functions are looked up but never called.
        public IntPtr Handle => new(1);
        public IntPtr GetInstanceProcAddress(IntPtr instance, string name) => new(1);
        public IntPtr GetDeviceProcAddress(IntPtr device, string name) => new(1);
        public IEnumerable<string> EnabledExtensions => Array.Empty<string>();
    }
}
