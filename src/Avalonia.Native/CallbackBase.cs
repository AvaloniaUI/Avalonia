using System;
using System.Runtime.ExceptionServices;
using Avalonia.MicroCom;
using Avalonia.Platform;
using MicroCom.Runtime;

namespace Avalonia.Native
{
    internal abstract class NativeCallbackBase : CallbackBase, IMicroComExceptionCallback
    {
        public void RaiseException(Exception e)
        {
            if (AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>() is PlatformThreadingInterface threadingInterface)
            {
                threadingInterface.TerminateNativeApp();

                threadingInterface.DispatchException(ExceptionDispatchInfo.Capture(e));
            }
        }
    }
}
