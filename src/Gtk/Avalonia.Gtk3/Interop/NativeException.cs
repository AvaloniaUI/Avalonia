using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Gtk3.Interop
{
    public class NativeException : Exception
    {
        public NativeException()
        {
        }

        public NativeException(string message) : base(message)
        {
        }

        public NativeException(string message, Exception inner) : base(message, inner)
        {
        }
        
    }
}
