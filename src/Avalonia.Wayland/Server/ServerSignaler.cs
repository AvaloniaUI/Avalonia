using System;

namespace Avalonia.Wayland.Server;

class ServerSignaler(WaylandWorker worker, Action callback)
{
    private object _lock = new object();
    private bool _signaled;
    public void Signal()
    {
        lock (_lock)
        {
            if (_signaled)
                return;
            _signaled = true;
            worker.PostOob(Callback);
        }
    }

    void Callback()
    {
        lock (_lock)
        {
            _signaled = false;
        }

        callback();
    }


}