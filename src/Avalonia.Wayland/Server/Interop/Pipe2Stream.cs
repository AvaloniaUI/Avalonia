using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Avalonia.Wayland.Server.Interop;

/// <summary>
/// A <see cref="PipeStream"/> backed by a file descriptor from <c>pipe2()</c>.
/// Sets <c>O_NONBLOCK</c> to enable async I/O via the .NET pipe infrastructure.
/// </summary>
class Pipe2Stream : PipeStream
{
    [DllImport("libc", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);

    private const int F_GETFL = 3;
    private const int F_SETFL = 4;
    private const int O_NONBLOCK = 0x800; // Linux x86_64

    public Pipe2Stream(int fd, PipeDirection direction)
        : base(direction, 0)
    {
        int flags = fcntl(fd, F_GETFL, 0);
        if (flags != -1)
            fcntl(fd, F_SETFL, flags | O_NONBLOCK);

        var handle = new SafePipeHandle((IntPtr)fd, ownsHandle: true);
        InitializeHandle(handle, isExposed: false, isAsync: true);
        IsConnected = true;
    }
}
