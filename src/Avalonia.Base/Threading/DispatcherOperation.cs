using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Threading;

public class DispatcherOperation
{
    protected readonly bool ThrowOnUiThread;
    public DispatcherOperationStatus Status { get; protected set; }
    public Dispatcher Dispatcher { get; }

    public DispatcherPriority Priority
    {
        get => _priority;
        set
        {
            _priority = value;
            // Dispatcher is null in ctor
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            Dispatcher?.SetPriority(this, value);
        }
    }

    protected object? Callback;
    protected object? TaskSource;
    
    internal DispatcherOperation? SequentialPrev { get; set; }
    internal DispatcherOperation? SequentialNext { get; set; }
    internal DispatcherOperation? PriorityPrev { get; set; }
    internal DispatcherOperation? PriorityNext { get; set; }
    internal PriorityChain? Chain { get; set; }
    
    internal bool IsQueued => Chain != null;

    private EventHandler? _aborted;
    private EventHandler? _completed;
    private DispatcherPriority _priority;

    internal DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Action callback, bool throwOnUiThread) :
        this(dispatcher, priority, throwOnUiThread)
    {
        Callback = callback;
    }

    private protected DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, bool throwOnUiThread)
    {
        ThrowOnUiThread = throwOnUiThread;
        Priority = priority;
        Dispatcher = dispatcher;
    }

    /// <summary>
    ///     An event that is raised when the operation is aborted or canceled.
    /// </summary>
    public event EventHandler Aborted
    {
        add
        {
            lock (Dispatcher.InstanceLock)
            {
                _aborted += value;
            }
        }

        remove
        {
            lock(Dispatcher.InstanceLock)
            {
                _aborted -= value;
            }
        }
    }

    /// <summary>
    ///     An event that is raised when the operation completes.
    /// </summary>
    /// <remarks>
    ///     Completed indicates that the operation was invoked and has
    ///     either completed successfully or faulted. Note that a canceled
    ///     or aborted operation is never is never considered completed.
    /// </remarks>
    public event EventHandler Completed
    {
        add
        {
            lock (Dispatcher.InstanceLock)
            {
                _completed += value;
            }
        }
        
        remove
        {
            lock(Dispatcher.InstanceLock)
            {
                _completed -= value;
            }
        }
    }
    
    public void Abort()
    {
        lock (Dispatcher.InstanceLock)
        {
            if (Status == DispatcherOperationStatus.Pending)
                return;
            Dispatcher.Abort(this);
        }
    }

    public void Wait()
    {
        if (Dispatcher.CheckAccess())
            throw new InvalidOperationException("Wait is only supported on background thread");
        GetTask().Wait();
    }

    public Task GetTask() => GetTaskCore();
    
    /// <summary>
    ///     Returns an awaiter for awaiting the completion of the operation.
    /// </summary>
    /// <remarks>
    ///     This method is intended to be used by compilers.
    /// </remarks>
    [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
    public TaskAwaiter GetAwaiter()
    {
        return GetTask().GetAwaiter();
    }

    internal void DoAbort()
    {
        Status = DispatcherOperationStatus.Aborted;
        AbortTask();
        _aborted?.Invoke(this, EventArgs.Empty);
    }
    
    internal void Execute()
    {
        lock (Dispatcher.InstanceLock)
        {
            Status = DispatcherOperationStatus.Executing;
        }

        try
        {
            InvokeCore();
        }
        finally
        {
            _completed?.Invoke(this, EventArgs.Empty);
        }
    }
    
    protected virtual void InvokeCore()
    {
        try
        {
            ((Action)Callback!)();
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                if (TaskSource is TaskCompletionSource<object?> tcs)
                    tcs.SetResult(null);
            }
        }
        catch (Exception e)
        {
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                if (TaskSource is TaskCompletionSource<object?> tcs)
                    tcs.SetException(e);
            }

            if (ThrowOnUiThread)
                throw;
        }
    }

    internal virtual object? GetResult() => null;
    
    protected virtual void AbortTask() => (TaskSource as TaskCompletionSource<object?>)?.SetCanceled();

    private static CancellationToken CreateCancelledToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    private static readonly Task s_abortedTask = Task.FromCanceled(CreateCancelledToken());

    protected virtual Task GetTaskCore()
    {
        lock (Dispatcher.InstanceLock)
        {
            if (Status == DispatcherOperationStatus.Aborted)
                return s_abortedTask;
            if (Status == DispatcherOperationStatus.Completed)
                return Task.CompletedTask;
            if (TaskSource is not TaskCompletionSource<object?> tcs)
                TaskSource = tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return tcs.Task;
        }
    }
}

public class DispatcherOperation<T> : DispatcherOperation
{
    public DispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, Func<T> callback) : base(dispatcher, priority, false)
    {
        TaskSource = new TaskCompletionSource<T>();
        Callback = callback;
    }

    private TaskCompletionSource<T> TaskCompletionSource => (TaskCompletionSource<T>)TaskSource!;

    public new Task<T> GetTask() => TaskCompletionSource!.Task;

    protected override Task GetTaskCore() => GetTask();

    protected override void AbortTask() => TaskCompletionSource.SetCanceled();

    internal override object? GetResult() => GetTask().Result;

    protected override void InvokeCore()
    {
        try
        {
            var result = ((Func<T>)Callback!)();
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                TaskCompletionSource.SetResult(result);
            }
        }
        catch (Exception e)
        {
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                TaskCompletionSource.SetException(e);
            }
        }
    }

    public T Result
    {
        get
        {
            if (TaskCompletionSource.Task.IsCompleted)
                return TaskCompletionSource.Task.GetAwaiter().GetResult();
            throw new InvalidOperationException("Synchronous wait is only supported on non-UI threads");

        }
    }
}

internal class SendOrPostCallbackDispatcherOperation : DispatcherOperation
{
    private readonly object? _arg;

    internal SendOrPostCallbackDispatcherOperation(Dispatcher dispatcher, DispatcherPriority priority, 
        SendOrPostCallback callback, object? arg, bool throwOnUiThread) 
        : base(dispatcher, priority, throwOnUiThread)
    {
        Callback = callback;
        _arg = arg;
    }
    
    protected override void InvokeCore()
    {
        try
        {
            ((SendOrPostCallback)Callback!)(_arg);
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                if (TaskSource is TaskCompletionSource<object?> tcs)
                    tcs.SetResult(null);
            }
        }
        catch (Exception e)
        {
            lock (Dispatcher.InstanceLock)
            {
                Status = DispatcherOperationStatus.Completed;
                if (TaskSource is TaskCompletionSource<object?> tcs)
                    tcs.SetException(e);
            }

            if (ThrowOnUiThread)
                throw;
        }
    }
}

public enum DispatcherOperationStatus
{
    Pending = 0,
    Aborted = 1,
    Completed = 2,
    Executing = 3,
}