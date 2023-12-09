#if NET6_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeWrapper : IDynamicInterfaceCastable, IDisposable
    {
        private bool _disposed;

        public static AutomationNodeWrapper? Create(IntPtr ptr)
        {
            Guid iid;

            iid = IRawElementProviderSimple.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderSimpleInst);

            iid = IRawElementProviderSimple2.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderSimple2Inst);

            iid = IRawElementProviderFragment.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderFragmentInst);

            iid = IInvokeProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IInvokeProviderInst);

            iid = IExpandCollapseProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IExpandCollapseProviderInst);

            iid = IRangeValueProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IRangeValueProviderInst);

            iid = IScrollProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IScrollProviderInst);

            iid = IScrollItemProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IScrollItemProviderInst);

            iid = ISelectionProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var ISelectionProviderInst);

            iid = ISelectionItemProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var ISelectionItemProviderInst);

            iid = IToggleProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IToggleProviderInst);

            iid = IValueProvider.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IValueProviderInst);

            iid = IRawElementProviderFragmentRoot.IID;
            Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderFragmentRootInst);

            return new AutomationNodeWrapper
            {
                IRawElementProviderSimpleInst = (void*)IRawElementProviderSimpleInst,
                IRawElementProviderSimple2Inst = (void*)IRawElementProviderSimple2Inst,
                IRawElementProviderFragmentInst = (void*)IRawElementProviderFragmentInst,
                IInvokeProviderInst = (void*)IInvokeProviderInst,
                IExpandCollapseProviderInst = (void*)IExpandCollapseProviderInst,
                IRangeValueProviderInst = (void*)IRangeValueProviderInst,
                IScrollProviderInst = (void*)IScrollProviderInst,
                IScrollItemProviderInst = (void*)IScrollItemProviderInst,
                ISelectionProviderInst = (void*)ISelectionProviderInst,
                ISelectionItemProviderInst = (void*)ISelectionItemProviderInst,
                IToggleProviderInst = (void*)IToggleProviderInst,
                IValueProviderInst = (void*)IValueProviderInst,
                IRawElementProviderFragmentRootInst = (void*)IRawElementProviderFragmentRootInst
            };
        }

        public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
        {
            if (interfaceType.Equals(typeof(IRawElementProviderSimple).TypeHandle) && IRawElementProviderSimpleInst != null)
                return typeof(IRawElementProviderSimpleNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRawElementProviderSimple2).TypeHandle) && IRawElementProviderSimple2Inst != null)
                return typeof(IRawElementProviderSimple2NativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRawElementProviderFragment).TypeHandle) && IRawElementProviderFragmentInst != null)
                return typeof(IRawElementProviderFragmentNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IInvokeProvider).TypeHandle) && IInvokeProviderInst != null)
                return typeof(IInvokeProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IExpandCollapseProvider).TypeHandle) && IExpandCollapseProviderInst != null)
                return typeof(IExpandCollapseProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRangeValueProvider).TypeHandle) && IRangeValueProviderInst != null)
                return typeof(IRangeValueProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IScrollProvider).TypeHandle) && IScrollProviderInst != null)
                return typeof(IScrollProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IScrollItemProvider).TypeHandle) && IScrollItemProviderInst != null)
                return typeof(IScrollItemProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(ISelectionProvider).TypeHandle) && ISelectionProviderInst != null)
                return typeof(ISelectionProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(ISelectionItemProvider).TypeHandle) && ISelectionItemProviderInst != null)
                return typeof(ISelectionItemProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IToggleProvider).TypeHandle) && IToggleProviderInst != null)
                return typeof(IToggleProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IValueProvider).TypeHandle) && IValueProviderInst != null)
                return typeof(IValueProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRawElementProviderFragmentRoot).TypeHandle) && IRawElementProviderFragmentRootInst != null)
                return typeof(IRawElementProviderFragmentRootNativeWrapper).TypeHandle;

            return default;
        }

        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (interfaceType.Equals(typeof(IRawElementProviderSimple).TypeHandle) && IRawElementProviderSimpleInst != null)
                return true;
            if (interfaceType.Equals(typeof(IRawElementProviderSimple2).TypeHandle) && IRawElementProviderSimple2Inst != null)
                return true;
            if (interfaceType.Equals(typeof(IRawElementProviderFragment).TypeHandle) && IRawElementProviderFragmentInst != null)
                return true;
            if (interfaceType.Equals(typeof(IInvokeProvider).TypeHandle) && IInvokeProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IExpandCollapseProvider).TypeHandle) && IExpandCollapseProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IRangeValueProvider).TypeHandle) && IRangeValueProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IScrollProvider).TypeHandle) && IScrollProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IScrollItemProvider).TypeHandle) && IScrollItemProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(ISelectionProvider).TypeHandle) && ISelectionProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(ISelectionItemProvider).TypeHandle) && ISelectionItemProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IToggleProvider).TypeHandle) && IToggleProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IValueProvider).TypeHandle) && IValueProviderInst != null)
                return true;
            if (interfaceType.Equals(typeof(IRawElementProviderFragmentRoot).TypeHandle) && IRawElementProviderFragmentRootInst != null)
                return true;

            return throwIfNotImplemented
                ? throw new InvalidCastException($"{nameof(AutomationNodeWrapper)} doesn't support {interfaceType}")
                : false;
        }

        ~AutomationNodeWrapper()
        {
            DisposeInternal();
        }

        public void Dispose()
        {
            DisposeInternal();
            GC.SuppressFinalize(this);
        }

        private void DisposeInternal()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (IRawElementProviderSimpleInst != null)
                Marshal.Release((IntPtr)IRawElementProviderSimpleInst);
            if (IRawElementProviderSimple2Inst != null)
                Marshal.Release((IntPtr)IRawElementProviderSimple2Inst);
            if (IRawElementProviderFragmentInst != null)
                Marshal.Release((IntPtr)IRawElementProviderFragmentInst);
            if (IInvokeProviderInst != null)
                Marshal.Release((IntPtr)IInvokeProviderInst);
            if (IExpandCollapseProviderInst != null)
                Marshal.Release((IntPtr)IExpandCollapseProviderInst);
            if (IRangeValueProviderInst != null)
                Marshal.Release((IntPtr)IRangeValueProviderInst);
            if (IScrollProviderInst != null)
                Marshal.Release((IntPtr)IScrollProviderInst);
            if (IScrollItemProviderInst != null)
                Marshal.Release((IntPtr)IScrollItemProviderInst);
            if (ISelectionProviderInst != null)
                Marshal.Release((IntPtr)ISelectionProviderInst);
            if (ISelectionItemProviderInst != null)
                Marshal.Release((IntPtr)ISelectionItemProviderInst);
            if (IToggleProviderInst != null)
                Marshal.Release((IntPtr)IToggleProviderInst);
            if (IValueProviderInst != null)
                Marshal.Release((IntPtr)IValueProviderInst);
            if (IRawElementProviderFragmentRootInst != null)
                Marshal.Release((IntPtr)IRawElementProviderFragmentRootInst);
        }

        public static T InvokeAndGet<T>(void* @this, int vtblSlot) where T : unmanaged
        {
            T ret;
            int hr = ((delegate* unmanaged<void*, T*, int>)(*(*(void***)@this + vtblSlot)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret;
        }

        public static TR InvokeAndGet<T1, TR>(void* @this, int vtblSlot, T1 value) where TR : unmanaged
        {
            TR ret;
            int hr = ((delegate* unmanaged<void*, T1, TR*, int>)(*(*(void***)@this + vtblSlot)))(@this, value, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret;
        }

        public static TR InvokeAndGet<T1, T2, TR>(void* @this, int vtblSlot, T1 value1, T2 value2) where TR : unmanaged
        {
            TR ret;
            int hr = ((delegate* unmanaged<void*, T1, T2, TR*, int>)(*(*(void***)@this + vtblSlot)))(@this, value1, value2, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret;
        }

        public static void Invoke(void* @this, int vtblSlot)
        {
            int hr = ((delegate* unmanaged<void*, int>)(*(*(void***)@this + vtblSlot)))(@this);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static void Invoke<T1>(void* @this, int vtblSlot, T1 value) where T1 : unmanaged
        {
            int hr = ((delegate* unmanaged<void*, T1, int>)(*(*(void***)@this + vtblSlot)))(@this, value);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static void Invoke<T1, T2>(void* @this, int vtblSlot, T1 value1, T2 value2) where T1 : unmanaged where T2 : unmanaged
        {
            int hr = ((delegate* unmanaged<void*, T1, T2, int>)(*(*(void***)@this + vtblSlot)))(@this, value1, value2);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }


        [DllImport("OleAut32.dll")]
        internal static extern SAFEARRAY* SafeArrayCreate(int vt, uint cDims, SAFEARRAYBOUND* rgsabound);

        [DllImport("OleAut32.dll")]
        internal static extern void SafeArrayAccessData(SAFEARRAY* psa, void** ppvData);

        [DllImport("OleAut32.dll")]
        internal static extern void SafeArrayUnaccessData(SAFEARRAY* psa);

        [DllImport("OleAut32.dll")]
        internal static extern void SafeArrayDestroy(SAFEARRAY* psa);

        internal const int VT_I4 = 3;
        internal const int VT_UNKNOWN = 13;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct SAFEARRAY
    {
        public ushort cDims;
        public ushort fFeatures;
        public ulong cbElements;
        public ulong cLocks;
        public void* pvData;
        public SAFEARRAYBOUND* rgsabound;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SAFEARRAYBOUND
    {
        public ulong cElements;
        public long lLbound;
    }

}
#endif
