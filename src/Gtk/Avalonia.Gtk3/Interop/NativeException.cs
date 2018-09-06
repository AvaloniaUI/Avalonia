using System;

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
