using Avalonia.Native.Interop;
using Avalonia.Threading;
using MicroCom.Runtime;

namespace Avalonia.Native;

class AvnDispatcher : NativeCallbackBase, IAvnDispatcher
{
    public void Post(IAvnActionCallback cb)
    {
        var callback = cb.CloneReference();
        Dispatcher.UIThread.Post(() =>
        {
            using (callback)
                callback.Run();
        }, DispatcherPriority.Send);
    }
}
