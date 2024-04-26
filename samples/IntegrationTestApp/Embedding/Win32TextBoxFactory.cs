using System;
using System.Text;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal class Win32TextBoxFactory : INativeControlFactory
{
    public IPlatformHandle CreateControl(IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        var handle = WinApi.CreateWindowEx(0, "EDIT",
            @"Native text box",
            (uint)(WinApi.WindowStyles.WS_CHILD | WinApi.WindowStyles.WS_VISIBLE | WinApi.WindowStyles.WS_BORDER), 
            0, 0, 1, 1, 
            parent.Handle,
            IntPtr.Zero, 
            WinApi.GetModuleHandle(null), 
            IntPtr.Zero);
        return new Win32WindowControlHandle(handle, "HWND");
    }
}
