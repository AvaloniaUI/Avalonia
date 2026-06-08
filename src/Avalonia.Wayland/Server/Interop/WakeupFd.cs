using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Server.Interop;
using static UnsafeNativeMethods;

unsafe class WakeupFd
{
    private readonly int _read;
    private readonly int _write;
    private readonly object _lock = new();
    private bool _signaled;

    public int PollFd => _read;

    public WakeupFd()
    {
        int* fds = stackalloc int[2];
        if (pipe2(fds, O_NONBLOCK | O_CLOEXEC) != 0)
            throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
        _read = fds[0];
        _write = fds[1];
    }

    private static readonly void* s_readBuf = (void*)Marshal.AllocHGlobal(1024);
    public void Clear()
    {
        lock (_lock)
        {
            if(!_signaled)
                return;
            var readNow = read(_read, s_readBuf, 1);
            Debug.Assert(readNow <= 1);
            _signaled = false;
        }
    }

    public void Set()
    {
        lock (_lock)
        {
            if(_signaled)
                return;
            byte b = 0;
            write(_write, &b, 1);
            _signaled = true;
        }
    }
}