using System;
using System.Collections.Generic;
using Avalonia.Reactive;
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
        VkQueue mainQueue, uint graphicsQueueIndex, string[] enabledExtensions)
    {
        _handle = handle;
        _physicalDeviceHandle = physicalDeviceHandle;
        _mainQueue = mainQueue;
        _graphicsQueueIndex = graphicsQueueIndex;
        Instance = instance;
        EnabledExtensions = enabledExtensions;
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

    public IEnumerable<string> EnabledExtensions { get; }

    public bool IsLost => false;
    public IntPtr Handle => _handle.Handle;
    public IntPtr PhysicalDeviceHandle => _physicalDeviceHandle.Handle;
    public IntPtr MainQueueHandle => CheckAccess(_mainQueue).Handle;
    public uint GraphicsQueueFamilyIndex => _graphicsQueueIndex;
    public IVulkanInstance Instance { get; }
    public void Dispose()
    {
        // TODO
    }

    public object? TryGetFeature(Type featureType) => null;
}