using System;
using System.Runtime.InteropServices;

namespace Avalonia.MicroCom
{
    public unsafe class MicroComProxyBase : IUnknown
    {
        private IntPtr _nativePointer;
        private bool _ownsHandle;

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
            if (rv != 0)
                return (T)MicroComRuntime.CreateProxyFor(typeof(T), ppv, true);
            throw new COMException("QueryInterface failed", rv);
        }

        public bool IsDisposed => _nativePointer == IntPtr.Zero;
        
        public void Dispose()
        {
            if (_ownsHandle)
                Release();
            _nativePointer = IntPtr.Zero;
        }

        public bool OwnsHandle => _ownsHandle;
        
        public void EnsureOwned()
        {
            if (!_ownsHandle)
            {
                AddRef();
                _ownsHandle = true;
            }
        }
    }
}
