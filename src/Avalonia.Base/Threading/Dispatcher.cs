using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Avalonia.Platform;

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
    internal IDispatcherClock Clock { get; }
    internal object InstanceLock { get; } = new();
    private bool _hasShutdownFinished;
    private IControlledDispatcherImpl? _controlledImpl;
    private static Dispatcher? s_uiThread;
    private IDispatcherImplWithPendingInput? _pendingInputImpl;

    internal Dispatcher(IDispatcherImpl impl, IDispatcherClock clock)
    {
        _impl = impl;
        Clock = clock;
        impl.Timer += OnOSTimer;
        impl.Signaled += Signaled;
        _controlledImpl = _impl as IControlledDispatcherImpl;
        _pendingInputImpl = _impl as IDispatcherImplWithPendingInput;
    }
    
    public static Dispatcher UIThread => s_uiThread ??= CreateUIThreadDispatcher();

    private static Dispatcher CreateUIThreadDispatcher()
    {
        var impl = AvaloniaLocator.Current.GetService<IDispatcherImpl>();
        if (impl == null)
        {
            var platformThreading = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
            if (platformThreading != null)
                impl = new LegacyDispatcherImpl(platformThreading);
            else
                impl = new NullDispatcherImpl();
        }
        return new Dispatcher(impl, impl as IDispatcherClock ?? new DefaultDispatcherClock());
    }

    /// <summary>
    /// Checks that the current thread is the UI thread.
    /// </summary>
    public bool CheckAccess() => _impl?.CurrentThreadIsLoopThread ?? true;

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
                => throw new InvalidOperationException("Call from invalid thread");

            ThrowVerifyAccess();
        }
    }

    internal void Shutdown()
    {
        DispatcherOperation? operation = null;
        _impl.Timer -= PromoteTimers;
        _impl.Signaled -= Signaled;
        do
        {
            lock(InstanceLock)
            {
                if(_queue.MaxPriority != DispatcherPriority.Invalid)
                {
                    operation = _queue.Peek();
                }
                else
                {
                    operation = null;
                }
            }

            if(operation != null)
            {
                operation.Abort();
            }
        } while(operation != null);
        _impl.UpdateTimer(null);
        _hasShutdownFinished = true;
    }

    /// <summary>
    /// Runs the dispatcher's main loop.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token used to exit the main loop.
    /// </param>
    public void MainLoop(CancellationToken cancellationToken)
    {
        if (_controlledImpl == null)
            throw new PlatformNotSupportedException();
        cancellationToken.Register(() => RequestProcessing());
        _controlledImpl.RunLoop(cancellationToken);
    }
}
