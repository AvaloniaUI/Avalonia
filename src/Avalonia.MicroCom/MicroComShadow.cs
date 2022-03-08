using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.MicroCom
{
    public unsafe class MicroComShadow : IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<Type, IntPtr> _shadows = new Dictionary<Type, IntPtr>();
        private readonly Dictionary<IntPtr, Type> _backShadows = new Dictionary<IntPtr, Type>();
        private GCHandle? _handle;
        private volatile int _refCount;
        internal IMicroComShadowContainer Target { get; }
        internal MicroComShadow(IMicroComShadowContainer target)
        {
            Target = target;
            Target.Shadow = this;
        }
        
        internal int QueryInterface(Ccw* ccw, Guid* guid, void** ppv)
        {
            if (MicroComRuntime.TryGetTypeForGuid(*guid, out var type))
                return QueryInterface(type, ppv);
            else if (*guid == MicroComRuntime.ManagedObjectInterfaceGuid)
            {
                ccw->RefCount++;
                *ppv = ccw;
                return 0;
            }
            else
                return unchecked((int)0x80004002u);
        }

        internal int QueryInterface(Type type, void** ppv)
        {
            if (!type.IsInstanceOfType(Target))
                return unchecked((int)0x80004002u);

            var rv = GetOrCreateNativePointer(type, ppv);
            if (rv == 0)
                AddRef((Ccw*)*ppv);
            return rv;
        }

        internal int GetOrCreateNativePointer(Type type, void** ppv)
        {
            if (!MicroComRuntime.GetVtableFor(type, out var vtable))
                return unchecked((int)0x80004002u);
            lock (_lock)
            {

                if (_shadows.TryGetValue(type, out var shadow))
                {
                    var targetCcw = (Ccw*)shadow;
                    AddRef(targetCcw);
                    *ppv = targetCcw;
                    return 0;
                }
                else
                {
                    var intPtr = Marshal.AllocHGlobal(Marshal.SizeOf<Ccw>());
                    var targetCcw = (Ccw*)intPtr;
                    *targetCcw = default;
                    targetCcw->RefCount = 0;
                    targetCcw->VTable = vtable;
                    if (_handle == null)
                        _handle = GCHandle.Alloc(this);
                    targetCcw->GcShadowHandle = GCHandle.ToIntPtr(_handle.Value);
                    _shadows[type] = intPtr;
                    _backShadows[intPtr] = type;
                    *ppv = targetCcw;

                    return 0;
                }
            }
        }

        internal int AddRef(Ccw* ccw)
        {
            if (Interlocked.Increment(ref _refCount) == 1)
            {
                try
                {
                    Target.OnReferencedFromNative();
                }
                catch (Exception e)
                {
                    MicroComRuntime.UnhandledException(Target, e);
                }
            }
            
            return Interlocked.Increment(ref ccw->RefCount);
        }

        internal int Release(Ccw* ccw)
        {
            Interlocked.Decrement(ref _refCount);
            var cnt = Interlocked.Decrement(ref ccw->RefCount);
            if (cnt == 0)
                return FreeCcw(ccw);

            return cnt;
        }

        int FreeCcw(Ccw* ccw)
        {
            lock (_lock)
            {
                // Shadow got resurrected by a call to QueryInterface from another thread
                if (ccw->RefCount != 0)
                    return ccw->RefCount;
                    
                var intPtr = new IntPtr(ccw);
                var type = _backShadows[intPtr];
                _backShadows.Remove(intPtr);
                _shadows.Remove(type);
                Marshal.FreeHGlobal(intPtr);
                if (_shadows.Count == 0)
                {
                    _handle?.Free();
                    _handle = null;
                    try
                    {
                        Target.OnUnreferencedFromNative();
                    }
                    catch(Exception e)
                    {
                        MicroComRuntime.UnhandledException(Target, e);
                    }
                }
            }

            return 0;
        }

        /*
         Needs to be called to support the following scenario:
         1) Object created
         2) Object passed to native code, shadow is created, CCW is created
         3) Native side has never called AddRef
         
         In that case the GC handle to the shadow object is still alive
         */
        
        public void Dispose()
        {
            lock (_lock)
            {
                List<IntPtr> toRemove = null;
                foreach (var kv in _backShadows)
                {
                    var ccw = (Ccw*)kv.Key;
                    if (ccw->RefCount == 0)
                    {
                        toRemove ??= new List<IntPtr>();
                        toRemove.Add(kv.Key);
                    }
                }

                if(toRemove != null)
                    foreach (var intPtr in toRemove)
                        FreeCcw((Ccw*)intPtr);
            }
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct Ccw
    {
        public IntPtr VTable;
        public IntPtr GcShadowHandle;
        public volatile int RefCount;
        public MicroComShadow GetShadow() => (MicroComShadow)GCHandle.FromIntPtr(GcShadowHandle).Target;
    }
}
