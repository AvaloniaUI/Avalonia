using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Headless;

/// <summary>
/// Headless unit test session that needs to be used by the actual testing framework.
/// All UI tests are supposed to be executed from the <see cref="Dispatcher"/> or <see cref="SynchronizationContext"/>
/// to keep execution flow on the UI thread.
/// Disposing unit test session stops internal dispatcher loop. 
/// </summary>
/// <remarks>
/// As Avalonia supports only a single Application instance created, this session must be created only once as well.
/// </remarks>
public sealed class HeadlessUnitTestSession : IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private static HeadlessUnitTestSession? s_session;
    private static object s_lock = new();
    private readonly BlockingCollection<Action> _queue;
    private readonly Task _dispatchTask;

    internal const DynamicallyAccessedMemberTypes DynamicallyAccessed =
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
    
    private HeadlessUnitTestSession(Type entryPointType, CancellationTokenSource cancellationTokenSource, BlockingCollection<Action> queue, Task _dispatchTask)
    {
        _cancellationTokenSource = cancellationTokenSource;
        _queue = queue;
        this._dispatchTask = _dispatchTask;
        EntryPointType = entryPointType;
    }

    internal Type EntryPointType { get; }

    public Task Dispatch(Action action, CancellationToken cancellationToken)
    {
        return Dispatch(() => { action(); return Task.FromResult(0); }, cancellationToken);
    }
    
    public Task<TResult> Dispatch<TResult>(Func<TResult> action, CancellationToken cancellationToken)
    {
        return Dispatch(() => Task.FromResult(action()), cancellationToken);
    }

    public Task<TResult> Dispatch<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            throw new ObjectDisposedException("Session was already disposed.");
        }

        var token = _cancellationTokenSource.Token;

        var tcs = new TaskCompletionSource<TResult>();
        _queue.Add(() =>
        {
            var cts = new CancellationTokenSource();
            using var globalCts = token.Register(s => ((CancellationTokenSource)s!).Cancel(), cts);
            using var localCts = cancellationToken.Register(s => ((CancellationTokenSource)s!).Cancel(), cts);

            try
            {
                var task = action();
                task.ContinueWith((_, s) => ((CancellationTokenSource)s!).Cancel(), cts);

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                var frame = new DispatcherFrame();
                using var innerCts = cts.Token.Register(() => frame.Continue = false);
                Dispatcher.UIThread.PushFrame(frame);

                var result = task.GetAwaiter().GetResult();
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }
    
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _queue.CompleteAdding();
        _dispatchTask.Wait();
        _cancellationTokenSource.Dispose();
    }

    /// <summary>
    /// Creates instance of <see cref="HeadlessUnitTestSession"/>. 
    /// </summary>
    /// <typeparam name="TEntryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
    /// </typeparam>
    public static HeadlessUnitTestSession StartNew<
        [DynamicallyAccessedMembers(DynamicallyAccessed)] TEntryPointType>()
    {
        return StartNew(typeof(TEntryPointType));
    }
    
    /// <summary>
    /// Creates instance of <see cref="HeadlessUnitTestSession"/>. 
    /// </summary>
    /// <param name="entryPointType">
    /// Parameter from which <see cref="AppBuilder"/> should be created.
    /// It either needs to have BuildAvaloniaApp -> AppBuilder method or inherit Application.
    /// </param>
    public static HeadlessUnitTestSession StartNew(
        [DynamicallyAccessedMembers(DynamicallyAccessed)] Type entryPointType)
    {
        var tcs = new TaskCompletionSource<HeadlessUnitTestSession>();
        var cancellationTokenSource = new CancellationTokenSource();
        var queue = new BlockingCollection<Action>();

        Task? task = null;
        task = Task.Run(() =>
        {
            try
            {
                var appBuilder = AppBuilder.Configure(entryPointType);

                // If windowing subsystem wasn't initialized by user, force headless with default parameters.
                if (appBuilder.WindowingSubsystemName != "Headless")
                {
                    appBuilder = appBuilder.UseHeadless(new AvaloniaHeadlessPlatformOptions());
                }

                appBuilder.SetupWithoutStarting();

                if (!Dispatcher.UIThread.SupportsRunLoops)
                {
                    throw new InvalidOperationException("Avalonia Headless platform has failed to initialize.");
                }

                // ReSharper disable once AccessToModifiedClosure
                tcs.SetResult(new HeadlessUnitTestSession(entryPointType, cancellationTokenSource, queue, task!));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var action = queue.Take(cancellationTokenSource.Token);
                    action();
                }
                catch (OperationCanceledException)
                {

                }
            }
        });

        return tcs.Task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Creates a session from AvaloniaTestApplicationAttribute attribute or reuses any existing.
    /// If AvaloniaTestApplicationAttribute doesn't exist, empty application is used. 
    /// </summary>
    /// <remarks>
    /// Note, only single session can be crated per app execution.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "AvaloniaTestApplicationAttribute attribute should preserve type information.")]
    public static HeadlessUnitTestSession GetOrStartForAssembly(Assembly? assembly)
    {
        lock (s_lock)
        {
            var appBuilderEntryPointType = assembly?.GetCustomAttribute<AvaloniaTestApplicationAttribute>()
                ?.AppBuilderEntryPointType;

            if (s_session is not null)
            {
                var hasNoAttribute = appBuilderEntryPointType == null && s_session.EntryPointType == typeof(Application);
                if (!hasNoAttribute && appBuilderEntryPointType != s_session.EntryPointType)
                {
                    // Avalonia doesn't support multiple Application instances. At least at the moment.
                    throw new System.InvalidOperationException(
                        "AvaloniaTestApplicationAttribute must be defined only once per single unit tests session.");
                }

                return s_session;
            }


            s_session = appBuilderEntryPointType is not null ? StartNew(appBuilderEntryPointType) : StartNew(typeof(Application));

            return s_session;
        }
    }
}
