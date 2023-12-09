#if NET6_0_OR_GREATER
using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Interop.Automation;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeWrapper : IDynamicInterfaceCastable, IDisposable
    {
        private bool _disposed;

        public bool IsRootAutomationNode { get; init; }

        public static AutomationNodeWrapper? CreateIfSupported(IntPtr ptr, bool rootAutomationNode)
        {
            Guid iid;
            int hr;

            iid = IRawElementProviderSimple.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderSimpleInst);
            if (hr != (int)HRESULT.S_OK)
            {
                return default;
            }

            iid = IRawElementProviderSimple2.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderSimple2Inst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IRawElementProviderSimpleInst);
                return default;
            }

            iid = IRawElementProviderFragment.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IRawElementProviderFragmentInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IRawElementProviderSimple2Inst);
                return default;
            }

            iid = IInvokeProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IInvokeProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IRawElementProviderFragmentInst);
                return default;
            }

            iid = IExpandCollapseProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IExpandCollapseProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IInvokeProviderInst);
                return default;
            }

            iid = IRangeValueProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IRangeValueProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IExpandCollapseProviderInst);
                return default;
            }

            iid = IScrollProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IScrollProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IRangeValueProviderInst);
                return default;
            }

            iid = IScrollItemProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IScrollItemProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IScrollProviderInst);
                return default;
            }

            iid = ISelectionProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var ISelectionProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IScrollItemProviderInst);
                return default;
            }

            iid = ISelectionItemProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var ISelectionItemProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(ISelectionProviderInst);
                return default;
            }

            iid = IToggleProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IToggleProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(ISelectionItemProviderInst);
                return default;
            }

            iid = IValueProvider.IID;
            hr = Marshal.QueryInterface(ptr, ref iid, out var IValueProviderInst);
            if (hr != (int)HRESULT.S_OK)
            {
                Marshal.Release(IToggleProviderInst);
                return default;
            }

            IntPtr IRawElementProviderFragmentRootInst = IntPtr.Zero;
            if (rootAutomationNode)
            {
                iid = IRawElementProviderFragmentRoot.IID;
                hr = Marshal.QueryInterface(ptr, ref iid, out IRawElementProviderFragmentRootInst);
                if (hr != (int)HRESULT.S_OK)
                {
                    Marshal.Release(IValueProviderInst);
                    return default;
                }
            }

            return new AutomationNodeWrapper
            {
                IsRootAutomationNode = rootAutomationNode,
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
            if (interfaceType.Equals(typeof(IRawElementProviderSimple).TypeHandle))
                return typeof(IRawElementProviderSimpleNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRawElementProviderSimple2).TypeHandle))
                return typeof(IRawElementProviderSimple2NativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRawElementProviderFragment).TypeHandle))
                return typeof(IRawElementProviderFragmentNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IInvokeProvider).TypeHandle))
                return typeof(IInvokeProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IExpandCollapseProvider).TypeHandle))
                return typeof(IExpandCollapseProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IRangeValueProvider).TypeHandle))
                return typeof(IRangeValueProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IScrollProvider).TypeHandle))
                return typeof(IScrollProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IScrollItemProvider).TypeHandle))
                return typeof(IScrollItemProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(ISelectionProvider).TypeHandle))
                return typeof(ISelectionProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(ISelectionItemProvider).TypeHandle))
                return typeof(ISelectionItemProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IToggleProvider).TypeHandle))
                return typeof(IToggleProviderNativeWrapper).TypeHandle;
            if (interfaceType.Equals(typeof(IValueProvider).TypeHandle))
                return typeof(IValueProviderNativeWrapper).TypeHandle;
            if (IsRootAutomationNode && interfaceType.Equals(typeof(IRawElementProviderFragmentRoot).TypeHandle))
                return typeof(IRawElementProviderFragmentRootNativeWrapper).TypeHandle;

            return default;
        }

        public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
        {
            if (interfaceType.Equals(typeof(IRawElementProviderSimple).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IRawElementProviderSimple2).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IRawElementProviderFragment).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IInvokeProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IExpandCollapseProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IRangeValueProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IScrollProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IScrollItemProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(ISelectionProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(ISelectionItemProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IToggleProvider).TypeHandle))
                return true;
            if (interfaceType.Equals(typeof(IValueProvider).TypeHandle))
                return true;
            if (IsRootAutomationNode && interfaceType.Equals(typeof(IRawElementProviderFragmentRoot).TypeHandle))
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

            Marshal.Release((IntPtr)IRawElementProviderSimpleInst);
            Marshal.Release((IntPtr)IRawElementProviderSimple2Inst);
            Marshal.Release((IntPtr)IRawElementProviderFragmentInst);
            Marshal.Release((IntPtr)IInvokeProviderInst);
            Marshal.Release((IntPtr)IExpandCollapseProviderInst);
            Marshal.Release((IntPtr)IRangeValueProviderInst);
            Marshal.Release((IntPtr)IScrollProviderInst);
            Marshal.Release((IntPtr)IScrollItemProviderInst);
            Marshal.Release((IntPtr)ISelectionProviderInst);
            Marshal.Release((IntPtr)ISelectionItemProviderInst);
            Marshal.Release((IntPtr)IToggleProviderInst);
            Marshal.Release((IntPtr)IValueProviderInst);

            if (IsRootAutomationNode)
            {
                Marshal.Release((IntPtr)IRawElementProviderFragmentRootInst);
            }
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
