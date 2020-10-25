using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Avalonia.MicroCom
{
    public static unsafe class MicroComRuntime
    {
        private static ConcurrentDictionary<Type, IntPtr> _vtables = new ConcurrentDictionary<Type, IntPtr>();

        private static ConcurrentDictionary<Type, Func<IntPtr, bool, object>> _factories =
            new ConcurrentDictionary<Type, Func<IntPtr, bool, object>>();
        private static ConcurrentDictionary<Type, Guid> _guids = new ConcurrentDictionary<Type, Guid>();
        private static ConcurrentDictionary<Guid, Type> _guidsToTypes = new ConcurrentDictionary<Guid, Type>();
        
        static MicroComRuntime()
        {
            Register(typeof(IUnknown), new Guid("00000000-0000-0000-C000-000000000046"),
                (ppv, owns) => new MicroComProxyBase(ppv, owns));
            RegisterVTable(typeof(IUnknown), MicroComVtblBase.Vtable);
        }

        public static void RegisterVTable(Type t, IntPtr vtable)
        {
            _vtables[t] = vtable;
        }
        
        public static void Register(Type t, Guid guid, Func<IntPtr, bool, object> proxyFactory)
        {
            _factories[t] = proxyFactory;
            _guids[t] = guid;
            _guidsToTypes[guid] = t;
        }

        public static Guid GetGuidFor(Type type) => _guids[type];

        public static T CreateProxyFor<T>(void* ppv, bool ownsHandle) => (T)CreateProxyFor(typeof(T), new IntPtr(ppv), ownsHandle);
        
        public static object CreateProxyFor(Type type, IntPtr ppv, bool ownsHandle) => _factories[type](ppv, ownsHandle);
        
        public static void* GetNativePointer<T>(T obj, bool owned = false) where T : IUnknown
        {
            if (obj is MicroComProxyBase proxy)
                return (void*)proxy.NativePointer;
            if (obj is IMicroComShadowContainer container)
            {
                container.Shadow ??= new MicroComShadow(container);
                void* ptr = null;
                var res = container.Shadow.GetOrCreateNativePointer(typeof(T), &ptr);
                if (res != 0)
                    throw new COMException(
                        "Unable to create native callable wrapper for type " + typeof(T) + " for instance of type " +
                        obj.GetType(),
                        res);
                if (owned)
                    container.Shadow.AddRef((Ccw*)ptr);
            }
            throw new ArgumentException("Unable to get a native pointer for " + obj);
        }

        public static object GetObjectFromCcw(IntPtr ccw)
        {
            var ptr = (Ccw*)ccw;
            var shadow = (MicroComShadow)GCHandle.FromIntPtr(ptr->GcShadowHandle).Target;
            return shadow.Target;
        }

        public static bool TryGetTypeForGuid(Guid guid, out Type t) => _guidsToTypes.TryGetValue(guid, out t);

        public static bool GetVtableFor(Type type, out IntPtr ptr) => _vtables.TryGetValue(type, out ptr);

        public static void UnhandledException(object target, Exception e)
        {
            if (target is IMicroComExceptionCallback cb)
            {
                try
                {
                    cb.RaiseException(e);
                }
                catch
                {
                    // We've tried
                }
            }

        }
    }
}
