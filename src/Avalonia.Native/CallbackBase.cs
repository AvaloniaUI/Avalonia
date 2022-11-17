using System;
using System.Runtime.ExceptionServices;
using Avalonia.MicroCom;
using Avalonia.Platform;

namespace Avalonia.Native
{
    public class CallbackBase : IUnknown, IMicroComShadowContainer, IMicroComExceptionCallback
    {
        private readonly object _lock = new object();
        private bool _referencedFromManaged = true;
        private bool _referencedFromNative = false;
        private bool _destroyed;
        

        protected virtual void Destroyed()
        {

        }

        public void RaiseException(Exception e)
        {
            if (AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>() is PlatformThreadingInterface threadingInterface)
            {
                threadingInterface.TerminateNativeApp();

                threadingInterface.DispatchException(ExceptionDispatchInfo.Capture(e));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _referencedFromManaged = false;
                DestroyIfNeeded();
            }
        }

        void DestroyIfNeeded()
        {
            if(_destroyed)
                return;
            if (_referencedFromManaged == false && _referencedFromNative == false)
            {
                _destroyed = true;
                Shadow?.Dispose();
                Shadow = null;
                Destroyed();
            }
        }

        public MicroComShadow Shadow { get; set; }
        public void OnReferencedFromNative()
        {
            lock (_lock) 
                _referencedFromNative = true;
        }

        public void OnUnreferencedFromNative()
        {
            lock (_lock)
            {
                _referencedFromNative = false;
                DestroyIfNeeded();
            }
        }
    }
}
