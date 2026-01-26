using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;

namespace Avalonia.Browser.Rendering;

internal partial class RenderWorker
{
    [DllImport("*")]
    private static extern int pthread_self();

    [JSImport("WebRenderTargetRegistry.initializeWorker", AvaloniaModule.MainModuleName)]
    private static partial void InitializeRenderTargets(); 
    
    internal static int WorkerThreadId;

    // The worker task needs to be rooted otherwise the web worker will exit.
    private static Task? s_workerTask;
    
    public static Task InitializeAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        s_workerTask = JSWebWorkerRunAsync(null, async () =>
        {
            try
            {
                await AvaloniaModule.ImportMainToCurrentContext();
                InitializeRenderTargets();
                WorkerThreadId = pthread_self();
                BrowserSharedRenderLoop.RenderTimer.StartOnThisThread();
                tcs.SetResult();
                // Never surrender
                await new TaskCompletionSource().Task;
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        
        s_workerTask.ContinueWith(_ =>
        {
            if (s_workerTask.IsFaulted)
                tcs.TrySetException(s_workerTask.Exception);
        });
        return tcs.Task;
    }

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RunAsync")]
    private static extern Task JSWebWorkerRunAsync(
        [UnsafeAccessorType("System.Runtime.InteropServices.JavaScript.JSWebWorker, System.Runtime.InteropServices.JavaScript")] object? instance,
        Func<Task> body);

}
