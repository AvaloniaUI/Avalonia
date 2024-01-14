using System;
using System.Runtime.Versioning;
using Avalonia.Metal;
using Avalonia.Utilities;
using Metal;

namespace Avalonia.iOS.Metal;

internal class MetalDevice : IMetalDevice
{
    private readonly DisposableLock _syncRoot = new();

    public MetalDevice(IMTLDevice device)
    {
        Device = device;
        Queue = device.CreateCommandQueue()
            ?? throw new InvalidOperationException("IMTLCommandQueue is not available");
    }

    public IMTLDevice Device { get; }
    public IMTLCommandQueue Queue { get; }
    IntPtr IMetalDevice.Device => Device.Handle;
    IntPtr IMetalDevice.CommandQueue => Queue.Handle;

    public bool IsLost => false;

    public IDisposable EnsureCurrent() => _syncRoot.Lock();
    public object? TryGetFeature(Type featureType) => null;

    public void Dispose()
    {
        Queue.Dispose();
    }
}
