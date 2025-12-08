using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
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
    
    public static Task InitializeAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerTask = JSWebWorkerClone.RunAsync(async () =>
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
        
        workerTask.ContinueWith(_ =>
        {
            if (workerTask.IsFaulted)
                tcs.TrySetException(workerTask.Exception);
        });
        return tcs.Task;
    }

    public static class JSWebWorkerClone
    {
        private static readonly MethodInfo _setExtLoop;
        private static readonly MethodInfo _intallInterop;

        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Runtime.InteropServices.JavaScript.JSSynchronizationContext", 
            "System.Runtime.InteropServices.JavaScript")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, "System.Runtime.InteropServices.JavaScript.JSHostImplementation", 
            "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2036", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Private runtime API")]
        static JSWebWorkerClone()
        {
            var syncContext = typeof(System.Runtime.InteropServices.JavaScript.JSHost)
                .Assembly!.GetType("System.Runtime.InteropServices.JavaScript.JSSynchronizationContext")!;
            var hostImpl = typeof(System.Runtime.InteropServices.JavaScript.JSHost)
                .Assembly!.GetType("System.Runtime.InteropServices.JavaScript.JSHostImplementation")!;
            
            _setExtLoop = hostImpl.GetMethod("SetHasExternalEventLoop")!;
            _intallInterop = syncContext.GetMethod("InstallWebWorkerInterop")!;
        }

        public static Task RunAsync(Func<Task> run)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var th = new Thread(_ =>
            {
                _intallInterop.Invoke(null, [false, CancellationToken.None]);
                try
                {
                    run().ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                            tcs.TrySetException(t.Exception);
                        else if (t.IsCanceled)
                            tcs.TrySetCanceled();
                        else
                            tcs.TrySetResult();
                    });
                }
                catch(Exception e)
                {
                    tcs.TrySetException(e);
                }
            })
            {
                Name = "Manual JS worker"
            };
            _setExtLoop.Invoke(null, [th]);
#pragma warning disable CA1416
            th.Start();
#pragma warning restore CA1416
            return tcs.Task;
        }
        
    }
    
    // TODO: Use this class instead of JSWebWorkerClone once https://github.com/dotnet/runtime/issues/102010 is fixed
    // TODO12: It was fixed in .NET 10
    class JSWebWorkerWrapper
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "System.Runtime.InteropServices.JavaScript.JSWebWorker", 
            "System.Runtime.InteropServices.JavaScript")]
        [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2036", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Private runtime API")]
        [UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Private runtime API")]
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
