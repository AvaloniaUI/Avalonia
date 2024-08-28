using System;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace IntegrationTestApp.Embedding;

internal class Win32WindowControlHandle : PlatformHandle, INativeControlHostDestroyableControlHandle
{
    public Win32WindowControlHandle(IntPtr handle, string descriptor) : base(handle, descriptor) { }
    public void Destroy() => WinApi.DestroyWindow(Handle);
}
