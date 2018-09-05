using System;
using System.Runtime.InteropServices;

namespace Avalonia.Gtk3.Interop
{
    class Signal
    {
        class ConnectedSignal : IDisposable
        {
            private readonly GObject _instance;
            private GCHandle _handle;
            private readonly ulong _id;

            public ConnectedSignal(GObject instance, GCHandle handle, ulong id)
            {
                _instance = instance;
                Native.GObjectRef(instance);
                _handle = handle;
                _id = id;
            }

            public void Dispose()
            {
                if (_handle.IsAllocated)
                {
                    Native.GObjectUnref(_instance.DangerousGetHandle());
                    Native.GSignalHandlerDisconnect(_instance, _id);
                    _handle.Free();
                }
            }
        }

        public static IDisposable Connect<T>(GObject obj, string name, T handler) 
        {
            var handle = GCHandle.Alloc(handler);
            var ptr = Marshal.GetFunctionPointerForDelegate((Delegate)(object)handler);
            using (var utf = new Utf8Buffer(name))
            {
                var id = Native.GSignalConnectObject(obj, utf, ptr, IntPtr.Zero, 0);
                if (id == 0)
                    throw new ArgumentException("Unable to connect to signal " + name);
                return new ConnectedSignal(obj, handle, id);
            }
        }
    }
}
