using System.IO.Pipes;
using Microsoft.Win32.SafeHandles;

namespace Avalonia.Wayland.Server.Interop;

/// <summary>
/// A <see cref="PipeStream"/> backed by a file descriptor from <c>pipe2()</c>.
/// </summary>
class Pipe2Stream : PipeStream
{
    public Pipe2Stream(int fd, PipeDirection direction)
        : base(direction, 0)
    {
        var handle = new SafePipeHandle(fd, ownsHandle: true);
        InitializeHandle(handle, isExposed: false, isAsync: false);
        IsConnected = true;
    }
}
