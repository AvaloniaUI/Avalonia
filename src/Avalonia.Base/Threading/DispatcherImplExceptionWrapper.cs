using System;
using System.Threading;

namespace Avalonia.Threading;

internal class DispatcherImplExceptionWrapper : IControlledDispatcherImpl
{
    public DispatcherImplExceptionWrapper(IControlledDispatcherImpl inner)
    {
        _inner = inner;
    }
    
    private readonly IControlledDispatcherImpl _inner;
    public bool CurrentThreadIsLoopThread => _inner.CurrentThreadIsLoopThread;

    public void Signal()
    {
        _inner.Signal();
    }

    public event Action? Signaled
    {
        add => _inner.Signaled += value;
        remove => _inner.Signaled -= value;
    }

    public event Action? Timer
    {
        add => _inner.Timer += value;
        remove => _inner.Timer -= value;
    }

    public long Now => _inner.Now;

    public void UpdateTimer(long? dueTimeInMs)
    {
        _inner.UpdateTimer(dueTimeInMs);
    }

    public bool CanQueryPendingInput => _inner.CanQueryPendingInput;

    public bool HasPendingInput => _inner.HasPendingInput;

    public void RunLoop(CancellationToken token)
    {
        tryagainloop:
        try
        {
            _inner.RunLoop(token);
        }
        catch (Exception e)
        {
            //RxApp.DefaultExceptionHandler.OnNext(e);
            goto tryagainloop;
        }
    }
}
