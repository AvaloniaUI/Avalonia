﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
            return Connect<T>(obj, name, handler, IntPtr.Zero);
        }

        public static IDisposable Connect<T>(GObject obj, string name, T handler, IntPtr userData) 
        {
            var handle = GCHandle.Alloc(handler);
            var ptr = Marshal.GetFunctionPointerForDelegate((Delegate)(object)handler);
            using (var utf = new Utf8Buffer(name))
            {
                var id = Native.GSignalConnectObject(obj, utf, ptr, userData, 0);
                if (id == 0)
                    throw new ArgumentException("Unable to connect to signal " + name);
                return new ConnectedSignal(obj, handle, id);
            }
        }
    }
}
