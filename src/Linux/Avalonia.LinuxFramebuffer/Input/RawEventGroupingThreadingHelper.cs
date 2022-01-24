using System;
using System.Collections.Generic;
using Avalonia.Input.Raw;
using Avalonia.Threading;

namespace Avalonia.LinuxFramebuffer.Input;

internal class RawEventGroupingThreadingHelper : IDisposable
{
    private readonly RawEventGrouper _grouper;
    private readonly Queue<RawInputEventArgs> _rawQueue = new();
    private readonly Action _queueHandler;

    public RawEventGroupingThreadingHelper(Action<RawInputEventArgs> eventCallback)
    {
        _grouper = new RawEventGrouper(eventCallback);
        _queueHandler = QueueHandler;
    }

    private void QueueHandler()
    {
        lock (_rawQueue)
        {
            while (_rawQueue.Count > 0)
                _grouper.HandleEvent(_rawQueue.Dequeue());
        }
    }

    public void OnEvent(RawInputEventArgs args)
    {
        lock (_rawQueue)
        {
            _rawQueue.Enqueue(args);
            if (_rawQueue.Count == 1)
            {
                Dispatcher.UIThread.Post(_queueHandler, DispatcherPriority.Input);
            }
        }
    }

    public void Dispose() =>
        Dispatcher.UIThread.Post(() => _grouper.Dispose(), DispatcherPriority.Input + 1);
}