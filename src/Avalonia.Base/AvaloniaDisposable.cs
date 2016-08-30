using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia
{
    public abstract class AvaloniaDisposable : IDisposable
    {
#if DEBUG_DISPOSE
        public string DisposedAt { get; private set; }
#endif


        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
#if DEBUG_DISPOSE
            DisposedAt = AvaloniaLocator.Current.GetService<IRuntimePlatform>().GetStackTrace();
#endif
            DoDispose();
        }

        protected void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(GetType().FullName
#if DEBUG_DISPOSE
                    , "Disposed at: \n" + DisposedAt
#endif

                    );
        }

        protected abstract void DoDispose();
    }
}
