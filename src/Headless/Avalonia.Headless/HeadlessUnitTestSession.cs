using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    private readonly CancellationTokenSource _cancellationToken;
    private static HeadlessUnitTestSession? s_session;
    private static object s_lock = new();

    internal const DynamicallyAccessedMemberTypes DynamicallyAccessed =
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor;
    
    private HeadlessUnitTestSession(Type entryPointType, Application application,
        SynchronizationContext synchronizationContext,
        Dispatcher dispatcher, CancellationTokenSource cancellationToken)
    {
        _cancellationToken = cancellationToken;
        EntryPointType = entryPointType;
        Dispatcher = dispatcher;
        Application = application;
        SynchronizationContext = synchronizationContext;
    }
    
    public Application Application { get; }
    public SynchronizationContext SynchronizationContext { get; }
    public Dispatcher Dispatcher { get; }
    internal Type EntryPointType { get; }

    public void Dispose()
    {
        _cancellationToken.Cancel();
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

        Thread? thread = null;
        thread = new Thread(() =>
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

                // ReSharper disable once AccessToModifiedClosure
                tcs.SetResult(new HeadlessUnitTestSession(entryPointType, Application.Current!,
                    SynchronizationContext.Current!, Dispatcher.UIThread, cancellationTokenSource));
            }
            catch (Exception e)
            {
                tcs.SetException(e);
                return;
            }

            Dispatcher.UIThread.MainLoop(cancellationTokenSource.Token);
        }) { IsBackground = true };
        thread.Start();

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
                if (appBuilderEntryPointType != s_session.EntryPointType)
                {
                    // Avalonia doesn't support multiple Application instances. At least at the moment.
                    throw new System.InvalidOperationException(
                        "AvaloniaTestApplicationAttribute must be defined only once per single unit tests session.");
                }

                return s_session;
            }


            s_session = appBuilderEntryPointType is not null ? StartNew(appBuilderEntryPointType).Result : StartNew(typeof(Application)).Result;

            return s_session;
        }
    }
}
