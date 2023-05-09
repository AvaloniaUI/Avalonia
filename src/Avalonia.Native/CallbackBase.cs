using System;
using System.Runtime.ExceptionServices;
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
            if (AvaloniaLocator.Current.GetService<IDispatcherImpl>() is DispatcherImpl dispatcherImpl)
            {
                dispatcherImpl.PropagateCallbackException(ExceptionDispatchInfo.Capture(e));
            }
        }
    }
}
