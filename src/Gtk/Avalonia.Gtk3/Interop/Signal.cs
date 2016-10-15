using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Gtk3.Interop
{
    public class Signal
    {
        class ConnectedSignal : IDisposable
        {
            private readonly IntPtr _instance;
            private GCHandle _handle;
            private readonly ulong _id;

            public ConnectedSignal(IntPtr instance, GCHandle handle, ulong id)
            {
                _instance = instance;
                _handle = handle;
                _id = id;
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                {
                    Native.GSignalHandlerDisconnect(_instance, _id);
                    _handle.Free();
                }
            }
        }

        public static IDisposable Connect<T>(IntPtr obj, string name, T handler) 
        {
            var handle = GCHandle.Alloc(handler);
            var ptr = Marshal.GetFunctionPointerForDelegate((Delegate)(object)handler);
            var id = Native.GSignalConnectObject(obj, name, ptr, IntPtr.Zero, 0);
            if (id == 0)
                throw new ArgumentException("Unable to connect to signal " + name);
            return new ConnectedSignal(obj, handle, id);
        }
    }
}
