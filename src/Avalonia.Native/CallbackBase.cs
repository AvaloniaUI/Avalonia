using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.MicroCom;
using Avalonia.Platform;
using Avalonia.Threading;
using MicroCom.Runtime;

namespace Avalonia.Native
{
    internal abstract class NativeCallbackBase : CallbackBase, IMicroComExceptionCallback
    {
        public void RaiseException(Exception e)
        {
            if(Dispatcher.FromThread(Thread.CurrentThread) is { PlatformImpl: DispatcherImpl dispatcherImpl })
            {
                dispatcherImpl.PropagateCallbackException(ExceptionDispatchInfo.Capture(e));
            }
        }
    }
}
