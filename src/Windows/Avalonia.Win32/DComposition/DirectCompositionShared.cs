using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Win32.Interop;
using MicroCom.Runtime;

namespace Avalonia.Win32.DComposition;

internal class DirectCompositionShared : IDisposable
{
    public object SyncRoot { get; } = new();

    public DirectCompositionShared(IDCompositionDesktopDevice device)
    {
        Device = device.CloneReference();
    }

    public IDCompositionDesktopDevice Device { get; }

    public void Dispose()
    {
        Device.Dispose();
    }
}
