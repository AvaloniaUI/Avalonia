using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

/// <summary>
///     A simple awaitable type that will return a DispatcherPriorityAwaiter.
/// </summary>
[UnconditionalSuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaitable
{
    private readonly Dispatcher _dispatcher;
    private readonly Task? _task;
    private readonly DispatcherPriority _priority;

    internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task? task, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        _task = task;
        _priority = priority;
    }

    public DispatcherPriorityAwaiter GetAwaiter() => new(_dispatcher, _task, _priority);
}

/// <summary>
///     A simple awaiter type that will queue the continuation to a dispatcher at a specific priority.
/// </summary>
/// <remarks>
///     This is returned from DispatcherPriorityAwaitable.GetAwaiter()
/// </remarks>
[UnconditionalSuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaiter : INotifyCompletion
{
    private readonly Dispatcher _dispatcher;
    private readonly Task? _task;
    private readonly DispatcherPriority _priority;

    internal DispatcherPriorityAwaiter(Dispatcher dispatcher, Task? task, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        _task = task;
        _priority = priority;
    }

    public void OnCompleted(Action continuation)
    {
        if(_task == null || _task.IsCompleted)
            _dispatcher.Post(continuation, _priority);
        else
        {
            var self = this;
            _task.ConfigureAwait(false).GetAwaiter().OnCompleted(() =>
            {
                self._dispatcher.Post(continuation, self._priority);
            });
        }
    }

    /// <summary>
    /// This always returns false since continuation is requested to be queued to a dispatcher queue
    /// </summary>
    public bool IsCompleted => false;

    public void GetResult()
    {
        if (_task != null)
            _task.GetAwaiter().GetResult();
    }
}

/// <summary>
///     A simple awaitable type that will return a DispatcherPriorityAwaiter&lt;T&gt;.
/// </summary>
[UnconditionalSuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaitable<T>
{
    private readonly Dispatcher _dispatcher;
    private readonly Task<T> _task;
    private readonly DispatcherPriority _priority;

    internal DispatcherPriorityAwaitable(Dispatcher dispatcher, Task<T> task, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        _task = task;
        _priority = priority;
    }

    public DispatcherPriorityAwaiter<T> GetAwaiter() => new(_dispatcher, _task, _priority);
}

/// <summary>
///     A simple awaiter type that will queue the continuation to a dispatcher at a specific priority.
/// </summary>
/// <remarks>
///     This is returned from DispatcherPriorityAwaitable&lt;T&gt;.GetAwaiter()
/// </remarks>
[UnconditionalSuppressMessage(
    "Performance",
    "CA1815:Override equals and operator equals on value types",
    Justification = "This struct is not supposed to be used directly and should not be compared.")]
public struct DispatcherPriorityAwaiter<T> : INotifyCompletion
{
    private readonly Dispatcher _dispatcher;
    private readonly Task<T> _task;
    private readonly DispatcherPriority _priority;

    internal DispatcherPriorityAwaiter(Dispatcher dispatcher, Task<T> task, DispatcherPriority priority)
    {
        _dispatcher = dispatcher;
        _task = task;
        _priority = priority;
    }

    public void OnCompleted(Action continuation)
    {
        if(_task.IsCompleted)
            _dispatcher.Post(continuation, _priority);
        else
        {
            var self = this;
            _task.ConfigureAwait(false).GetAwaiter().OnCompleted(() =>
            {
                self._dispatcher.Post(continuation, self._priority);
            });
        }
    }
    
    /// <summary>
    /// This always returns false since continuation is requested to be queued to a dispatcher queue
    /// </summary>
    public bool IsCompleted => false;

    public void GetResult() => _task.GetAwaiter().GetResult();
}
