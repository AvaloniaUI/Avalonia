using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace Avalonia.IntegrationTests.Win32.Infrastructure;

internal static class AppManager
{
    private static readonly Lazy<Task<Dispatcher>> s_initTask = new(CreateUIThread, LazyThreadSafetyMode.ExecutionAndPublication);
    private static readonly CancellationTokenSource s_cancellation = new();

    public static void Stop()
        => s_cancellation.Cancel();

    private static Task<Dispatcher> CreateUIThread()
    {
        var tcs = new TaskCompletionSource<Dispatcher>();

        var uiThread = new Thread(() =>
        {
            var appBuilder = AppBuilder
                .Configure<Application>()
                .UseWin32()
                .UseSkia()
                .SetupWithoutStarting();

            appBuilder.Instance!.Styles.Add(new FluentTheme());

            // Ensure that Dispatcher.UIThread is initialized on this thread
            var dispatcher = Dispatcher.UIThread;
            dispatcher.VerifyAccess();
            tcs.TrySetResult(dispatcher);

            dispatcher.MainLoop(s_cancellation.Token);
        })
        {
            Name = "UI Thread"
        };

        uiThread.Start();

        return tcs.Task;
    }

    public static Task<Dispatcher> EnsureAppInitializedAsync()
        => s_initTask.Value;
}
