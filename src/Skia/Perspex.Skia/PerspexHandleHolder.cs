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
}