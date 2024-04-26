using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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
    
    public static int WorkerThreadId;
    
    public static Task InitializeAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerTask = JSWebWorkerWrapper.RunAsync(async () =>
        {
            try
            {
                await AvaloniaModule.ImportMainToCurrentContext();
                InitializeRenderTargets();
                WorkerThreadId = pthread_self();
                BrowserCompositor.RenderTimer.StartOnThisThread();
                tcs.SetResult();
                // Never surrender
                await new TaskCompletionSource().Task;
            }
            catch (Exception e)
            {
                tcs.SetException(e);
            }
        });
        
        workerTask.ContinueWith(_ =>
        {
            if (workerTask.IsFaulted)
                tcs.TrySetException(workerTask.Exception);
        });
        return tcs.Task;
    }
    
    class JSWebWorkerWrapper
    {

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", 
            "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("Trimming", 
            "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
            Justification = "Private runtime API")]
        static JSWebWorkerWrapper()
        {
            var type = typeof(System.Runtime.InteropServices.JavaScript.JSHost)
                .Assembly!.GetType("System.Runtime.InteropServices.JavaScript.JSWebWorker");
#pragma warning disable IL2075
            var m = type!

                .GetMethods(BindingFlags.Static | BindingFlags.Public
                ).First(m => m.Name == "RunAsync"
                             && m.ReturnType == typeof(Task)
                             && m.GetParameters() is { } parameters
                             && parameters.Length == 1
                             && parameters[0].ParameterType == typeof(Func<Task>));

#pragma warning restore IL2075
            RunAsync = (Func<Func<Task>, Task>) Delegate.CreateDelegate(typeof(Func<Func<Task>, Task>), m);

        }

        public static Func<Func<Task>, Task> RunAsync { get; set; }
    }

}