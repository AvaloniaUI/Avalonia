using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Threading;

/// <summary>
/// Provides services for managing work items on a thread.
/// </summary>
/// <remarks>
/// In Avalonia, there is usually only a single <see cref="Dispatcher"/> in the application -
/// the one for the UI thread, retrieved via the <see cref="UIThread"/> property.
/// </remarks>
public partial class Dispatcher : IDispatcher
{
    private IDispatcherImpl _impl;
    private bool _initialized;
    internal object InstanceLock { get; } = new();
    private IControlledDispatcherImpl? _controlledImpl;
    private IDispatcherImplWithPendingInput? _pendingInputImpl;
    private IDispatcherImplWithExplicitBackgroundProcessing? _backgroundProcessingImpl;
    private readonly Thread _thread;

    private readonly AvaloniaSynchronizationContext?[] _priorityContexts =
        new AvaloniaSynchronizationContext?[DispatcherPriority.MaxValue - DispatcherPriority.MinValue + 1];

    internal Dispatcher(IDispatcherImpl? impl, Func<TimeSpan>? timeProvider = null)
    {
#if DEBUG
        if (AvaloniaLocator.Current.GetService<IDispatcherImpl>() != null
            || AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>() != null)
            throw new InvalidOperationException(
                "Registering IDispatcherImpl or IPlatformThreadingInterface via locator is no longer valid");
#endif
        lock (s_globalLock)
        {
            _thread = Thread.CurrentThread;
            if (FromThread(_thread) != null)
                throw new InvalidOperationException("The current thread already has a dispatcher");

            // The first created dispatcher becomes "UI thread one"
            s_uiThread ??= this;

            s_dispatchers.Remove(Thread.CurrentThread);
            s_dispatchers.Add(Thread.CurrentThread,
                s_currentThreadDispatcher = new() { Reference = new WeakReference<Dispatcher>(this) });
#if !NET6_0_OR_GREATER
        s_resetForTestsList.Add(s_currentThreadDispatcher);
#endif
        }
        
        var st = Stopwatch.StartNew();
        _timeProvider = timeProvider ?? (() => st.Elapsed);

        _impl = null!; // Set by ReplaceImplementation
        ReplaceImplementation(impl);
        

        _unhandledExceptionEventArgs = new DispatcherUnhandledExceptionEventArgs(this);
        _exceptionFilterEventArgs = new DispatcherUnhandledExceptionFilterEventArgs(this);
    }

    public bool SupportsRunLoops => _controlledImpl != null;

    /// <summary>
    /// Checks that the current thread is the UI thread.
    /// </summary>
    public bool CheckAccess() => Thread.CurrentThread == _thread;

    /// <summary>
    /// Checks that the current thread is the UI thread and throws if not.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The current thread is not the UI thread.
    /// </exception>
    public void VerifyAccess()
    {
        if (!CheckAccess())
        {
            // Used to inline VerifyAccess.
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static void ThrowVerifyAccess()
                => throw new InvalidOperationException("The calling thread cannot access this object because a different thread owns it.");
            ThrowVerifyAccess();
        }
    }

    public Thread Thread => _thread;

    internal AvaloniaSynchronizationContext GetContextWithPriority(DispatcherPriority priority)
    {
        DispatcherPriority.Validate(priority, nameof(priority));
        var index = priority - DispatcherPriority.MinValue;
        return _priorityContexts[index] ??= new(this, priority);
    }

    [PrivateApi]
    public IDispatcherImpl PlatformImpl => _impl;
    
    private void ReplaceImplementation(IDispatcherImpl? impl)
    {
        // TODO: Consider moving the helper out of Avalonia.Win32 so
        // it's usable earlier
        using var _ = NonPumpingLockHelper.Use();


        if (impl?.CurrentThreadIsLoopThread == false)
            throw new InvalidOperationException("IDispatcherImpl belongs to a different thread");
        
        if (_impl != null!) // Null in ctor
        {
            _impl.Timer -= OnOSTimer;
            _impl.Signaled -= Signaled;
            if (_backgroundProcessingImpl != null)
                _backgroundProcessingImpl.ReadyForBackgroundProcessing -= OnReadyForExplicitBackgroundProcessing;
            _impl = null!;
            _controlledImpl = null;
            _pendingInputImpl = null;
            _backgroundProcessingImpl = null;
        }

        if (impl != null)
            _initialized = true;
        else
            impl = new ManagedDispatcherImpl(null);
        _impl = impl;
        
        impl.Timer += OnOSTimer;
        impl.Signaled += Signaled;
        _controlledImpl = _impl as IControlledDispatcherImpl;
        _pendingInputImpl = _impl as IDispatcherImplWithPendingInput;
        _backgroundProcessingImpl = _impl as IDispatcherImplWithExplicitBackgroundProcessing;
        _maximumInputStarvationTime = _backgroundProcessingImpl == null ?
            MaximumInputStarvationTimeInFallbackMode :
            MaximumInputStarvationTimeInExplicitProcessingExplicitMode;
        if (_backgroundProcessingImpl != null)
            _backgroundProcessingImpl.ReadyForBackgroundProcessing += OnReadyForExplicitBackgroundProcessing;
        if(_signaled)
            _impl.Signal();
        _osTimerSetTo = null;
        UpdateOSTimer();
    }
}
