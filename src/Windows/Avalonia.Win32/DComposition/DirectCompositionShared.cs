using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositionShared : IDisposable
{
    private static readonly Guid IID_IDCompositionDevice = Guid.Parse("c37ea93a-e7aa-450d-b16f-9746cb0407f3");
    private IDCompositionDevice? _device;

    public object SyncRoot { get; } = new();

    public IDCompositionDevice? Device => _device;

    public void EnsureCompositionDevice(IntPtr d3dDevice)
    {
        if (_device is not null)
        {
            return;
        }
        
        // NOTE: we do not check if d3d device was changed, as in Avalonia we reuse same device for each windows.
        // But ideally there should be a strict 1 to 1 relation between d3ddevice and dcomp device. 
        var result = NativeMethods.DCompositionCreateDevice(d3dDevice, IID_IDCompositionDevice, out var cDevice);
        if (result != UnmanagedMethods.HRESULT.S_OK)
        {
            throw new Win32Exception((int)result);
        }
                
        using var device = MicroComRuntime.CreateProxyFor<IDCompositionDevice>(cDevice, false);
        _device = device.CloneReference();
    }
    
    public void Dispose()
    {
        _device?.Dispose();
    }
}
