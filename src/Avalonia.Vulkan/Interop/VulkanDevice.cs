using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Vulkan.Interop;
using Avalonia.Vulkan.UnmanagedInterop;
namespace Avalonia.Vulkan.Interop;

internal partial class VulkanDevice : IVulkanDevice
{
    private readonly VkDevice _handle;
    private readonly VkPhysicalDevice _physicalDeviceHandle;
    private readonly VkQueue _mainQueue;
    private readonly uint _graphicsQueueIndex;
    private readonly object _lock = new();
    private Thread? _lockedByThread;
    private int _lockCount;

    private VulkanDevice(IVulkanInstance instance, VkDevice handle, VkPhysicalDevice physicalDeviceHandle,
        VkQueue mainQueue, uint graphicsQueueIndex)
    {
        _handle = handle;
        _physicalDeviceHandle = physicalDeviceHandle;
        _mainQueue = mainQueue;
        _graphicsQueueIndex = graphicsQueueIndex;
        Instance = instance;
    }

    T CheckAccess<T>(T f)
    {
        if (_lockedByThread != Thread.CurrentThread)
            throw new InvalidOperationException("This class is only usable when locked");
        return f;
    }

    public IDisposable Lock()
    {
        Monitor.Enter(_lock);
        _lockCount++;
        _lockedByThread = Thread.CurrentThread;
        return Disposable.Create(() =>
        {
            _lockCount--;
            if (_lockCount == 0)
                _lockedByThread = null;
            Monitor.Exit(_lock);
        });
    }

    public bool IsLost => false;
    public IntPtr Handle => CheckAccess(_handle).Handle;
    public IntPtr PhysicalDeviceHandle => CheckAccess(_physicalDeviceHandle).Handle;
    public IntPtr MainQueueHandle => CheckAccess(_mainQueue).Handle;
    public uint GraphicsQueueFamilyIndex => CheckAccess(_graphicsQueueIndex);
    public IVulkanInstance Instance { get; }
    public void Dispose()
    {
        // TODO
    }

    public object? TryGetFeature(Type featureType) => null;
}