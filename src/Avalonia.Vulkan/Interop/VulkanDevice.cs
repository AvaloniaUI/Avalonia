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
        CheckIsDisposed(f);

        if (_lockedByThread != Thread.CurrentThread)
            throw new InvalidOperationException("This class is only usable when locked");
        return f;
    }

    private T CheckIsDisposed<T>(T f)
    {
        if (IsDisposed)
            throw new VulkanException("Cannot access a disposed VulkanDevice");

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
    public IntPtr Handle => CheckIsDisposed(_handle).Handle;
    public IntPtr PhysicalDeviceHandle => CheckIsDisposed(_physicalDeviceHandle).Handle;
    public IntPtr MainQueueHandle => CheckAccess(_mainQueue).Handle;
    public uint GraphicsQueueFamilyIndex => _graphicsQueueIndex;
    public IVulkanInstance Instance { get; }
    public bool IsDisposed { get; private set; }

    public object? TryGetFeature(Type featureType) => null;


    public void Dispose()
    {
        if (IsDisposed)
            return;

        if (_handle.Handle != IntPtr.Zero)
        {
            var api = new VulkanInstanceApi(Instance);
            api.DestroyDevice(_handle, IntPtr.Zero);
        }

        IsDisposed = true;
    }
}
