using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Threading
{
    public class DispatcherUnhandledExceptionEventArgs : EventArgs
    {
        public DispatcherUnhandledExceptionEventArgs(Exception exception)
        {
            Exception = exception;
        }

        public Exception Exception { get; }

        public bool IsHandled { get; set; }
    }
}
