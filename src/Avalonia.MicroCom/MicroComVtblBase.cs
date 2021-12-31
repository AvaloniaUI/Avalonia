using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Avalonia.MicroCom
{
    public unsafe class MicroComVtblBase
    {
        private List<IntPtr> _methods = new List<IntPtr>();
        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        private delegate int AddRefDelegate(Ccw* ccw);

        [UnmanagedFunctionPointerAttribute(CallingConvention.StdCall)]
        private delegate int QueryInterfaceDelegate(Ccw* ccw, Guid* guid, void** ppv);

        public static IntPtr Vtable { get; } = new MicroComVtblBase().CreateVTable();
        public MicroComVtblBase()
        {
            AddMethod((QueryInterfaceDelegate)QueryInterface);
            AddMethod((AddRefDelegate)AddRef);
            AddMethod((AddRefDelegate)Release);
        }

        protected void AddMethod(void* f)
        {
            _methods.Add(new IntPtr(f));
        }

        protected void AddMethod(Delegate d)
        {
            GCHandle.Alloc(d);
            _methods.Add(Marshal.GetFunctionPointerForDelegate(d));
        }

        protected unsafe IntPtr CreateVTable()
        {
            var ptr = (IntPtr*)Marshal.AllocHGlobal((IntPtr.Size + 1) * _methods.Count);
            for (var c = 0; c < _methods.Count; c++)
                ptr[c] = _methods[c];
            return new IntPtr(ptr);
        }

        static int QueryInterface(Ccw* ccw, Guid* guid, void** ppv) => ccw->GetShadow().QueryInterface(ccw, guid, ppv);
        static int AddRef(Ccw* ccw) => ccw->GetShadow().AddRef(ccw);
        static int Release(Ccw* ccw) => ccw->GetShadow().Release(ccw);
    }
}
