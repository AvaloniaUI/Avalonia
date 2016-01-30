using System;

namespace Perspex.Skia
{
    abstract class PerspexHandleHolder : IDisposable
    {
        private readonly IntPtr _handle;

        public IntPtr Handle
        {
            get
            {
                CheckDisposed();
                return _handle;
            }
        }

        public bool IsDisposed { get; private set; }

        public void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        protected PerspexHandleHolder(IntPtr handle)
        {
            _handle = handle;
        }

        protected abstract void Delete(IntPtr handle);

        public void Dispose()
        {
            if(IsDisposed)
                return;
            IsDisposed = true;
            Delete(_handle);
            GC.SuppressFinalize(this);
        }

        ~PerspexHandleHolder()
        {
            Dispose();
        }
    }

    class RefCountable<T> : IDisposable where T : PerspexHandleHolder
    {
        class Shared
        {
            public readonly T Target;
            private int _refCount = 1;

            public Shared(T target)
            {
                Target = target;
            }

            public void AddRef() => _refCount++;
            public void Release()
            {
                _refCount--;
                if (_refCount <= 0)
                    Target.Dispose();
            }
        }

        public bool IsDisposed => _shared == null;
        private Shared _shared;
        public void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName);
        }

        public IntPtr Handle
        {
            get
            {
                CheckDisposed();
                return _shared.Target.Handle;
            }
        }

        public RefCountable(T handle)
        {
            _shared = new Shared(handle);
        }

        public RefCountable(RefCountable<T> other)
        {
            other._shared.Target.CheckDisposed();
            other._shared.AddRef();
            _shared = other._shared;
        }

        public RefCountable<T> Clone() => new RefCountable<T>(this);

        public void Dispose()
        {
            _shared?.Release();
            _shared = null;
        }
    }
}