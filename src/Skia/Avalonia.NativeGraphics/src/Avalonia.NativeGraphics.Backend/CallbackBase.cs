using System;
using System.Runtime.ExceptionServices;
using Avalonia.Platform;
using MicroCom.Runtime;

namespace Avalonia.NativeGraphics.Backend
{
    internal class CallbackBase : IUnknown, IMicroComShadowContainer
    {
        private readonly object _lock = new object();
        private bool _referencedFromManaged = true;
        private bool _referencedFromNative = false;
        private bool _destroyed;
        
        protected virtual void Destroyed()
        {
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
                Destroyed();
            }
        }

        public MicroComShadow Shadow { get; set; }
        public virtual void OnReferencedFromNative()
        {
            lock (_lock) 
                _referencedFromNative = true;
        }

        public virtual void OnUnreferencedFromNative()
        {
            lock (_lock)
            {
                _referencedFromNative = false;
                DestroyIfNeeded();
            }
        }
    }
}