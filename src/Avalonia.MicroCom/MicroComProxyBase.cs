using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.MicroCom
{
    public unsafe class MicroComProxyBase : CriticalFinalizerObject, IUnknown
    {
        private IntPtr _nativePointer;
        private bool _ownsHandle;
        private SynchronizationContext _synchronizationContext;

        public IntPtr NativePointer
        {
            get
            {
                if (_nativePointer == IntPtr.Zero)
                    throw new ObjectDisposedException(this.GetType().FullName);
                return _nativePointer;
            }
        }

        public void*** PPV => (void***)NativePointer;

        public MicroComProxyBase(IntPtr nativePointer, bool ownsHandle)
        {
            _nativePointer = nativePointer;
            _ownsHandle = ownsHandle;
            _synchronizationContext = SynchronizationContext.Current;
            if(!_ownsHandle)
                GC.SuppressFinalize(this);
        }

        protected virtual int VTableSize => 3;
        
        public void AddRef()
        {
            LocalInterop.CalliStdCallvoid(PPV, (*PPV)[1]);
        }

        public void Release()
        {
            LocalInterop.CalliStdCallvoid(PPV, (*PPV)[2]);
        }

        public int QueryInterface(Guid guid, out IntPtr ppv)
        {
            IntPtr r = default;
            var rv = LocalInterop.CalliStdCallint(PPV, &guid, &r, (*PPV)[0]);
            ppv = r;
            return rv;
        }

        public T QueryInterface<T>() where T : IUnknown
        {
            var guid = MicroComRuntime.GetGuidFor(typeof(T));
            var rv = QueryInterface(guid, out var ppv);
            if (rv == 0)
                return (T)MicroComRuntime.CreateProxyFor(typeof(T), ppv, true);
            throw new COMException("QueryInterface failed", rv);
        }

        public bool IsDisposed => _nativePointer == IntPtr.Zero;

        protected virtual void Dispose(bool disposing)
        {
            if(_nativePointer == IntPtr.Zero)
                return;
            if (_ownsHandle)
            {
                Release();
                _ownsHandle = false;
            }
            _nativePointer = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }
        
        public void Dispose() => Dispose(true);

        public bool OwnsHandle => _ownsHandle;
        
        public void EnsureOwned()
        {
            if (!_ownsHandle)
            {
                GC.ReRegisterForFinalize(true);
                AddRef();
                _ownsHandle = true;
            }
        }

        private static readonly SendOrPostCallback _disposeDelegate = DisposeOnContext;

        private static void DisposeOnContext(object state)
        {
            (state as MicroComProxyBase)?.Dispose(false);
        }

        ~MicroComProxyBase()
        {
            if(!_ownsHandle)
                return;
            if (_synchronizationContext == null)
                Dispose();
            else
                _synchronizationContext.Post(_disposeDelegate, this);
        }
    }
}
