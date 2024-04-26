using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Avalonia.Browser.Interop;

public class JsCallbackHelper
{
    public static Task WrappedInvoke(Action cb)
    {
        if (!Dispatcher.UIThread.CheckAccess())
            // Sync callbacks in workers should be allowed, so we technically shouldn't be there?
            cb();
        try
        {
            Dispatcher.UIThread.Invoke(cb, DispatcherPriority.Input);
        }
        catch (Exception e)
        {
            // Not sure if there is a more sensible thing to do
            Console.Error.WriteLine(e.ToString());
        }
        return Task.CompletedTask;
    }
    
    
}