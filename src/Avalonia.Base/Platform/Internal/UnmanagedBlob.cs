using System;
using System.Runtime.InteropServices;

namespace Avalonia.Platform.Internal;

internal class UnmanagedBlob : IUnmanagedBlob
{
    private IntPtr _address;
    private readonly object _lock = new object();
    
    public UnmanagedBlob(int size)
    {
        try
        {
            if (size <= 0)
                throw new ArgumentException("Positive number required", nameof(size));
            _address = Marshal.AllocHGlobal(size);
            GC.AddMemoryPressure(size);
            Size = size;
        }
        catch
        {
            GC.SuppressFinalize(this);
            throw;
        }
    }
    
    private void DoDispose()
    {
        lock (_lock)
        {
            if (!IsDisposed)
            {
                Marshal.FreeHGlobal(_address);
                GC.RemoveMemoryPressure(Size);
                IsDisposed = true;
                _address = IntPtr.Zero;
                Size = 0;
            }
        }
    }
    
    public void Dispose()
    {
        DoDispose();
        GC.SuppressFinalize(this);
    }
    
    ~UnmanagedBlob()
    {
        DoDispose();
    }
    
    public IntPtr Address => IsDisposed ? throw new ObjectDisposedException("UnmanagedBlob") : _address;
    public int Size { get; private set; }
    public bool IsDisposed { get; private set; }
}
