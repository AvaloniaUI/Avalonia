using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRawElementProviderFragmentRoot : IRawElementProviderFragment
    {
#if NET6_0_OR_GREATER
        public static new readonly Guid IID = new("620ce2a5-ab8f-40a9-86cb-de3c75599b58");
        public new const int VtblSize = IRawElementProviderFragment.VtblSize + 2;
#endif
        IRawElementProviderFragment? ElementProviderFromPoint(double x, double y);
        IRawElementProviderFragment? GetFocus();
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRawElementProviderFragmentRootManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Navigate(void* @this, NavigateDirection direction, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).Navigate(direction);
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetRuntimeId(void* @this, SAFEARRAY** ret)
        {
            try
            {
                var arr = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).GetRuntimeId();
                if (arr is not null)
                {
                    var bounds = new SAFEARRAYBOUND
                    {
                        cElements = (ulong)arr.Length,
                        lLbound = 0
                    };

                    var safeArray = AutomationNodeWrapper.SafeArrayCreate(AutomationNodeWrapper.VT_I4, 1, &bounds);
                    void* pData;
                    AutomationNodeWrapper.SafeArrayAccessData(safeArray, &pData);
                    for (var i = 0; i < arr.Length; i++)
                    {
                        ((int*)pData)[i] = arr[i];
                    }
                    AutomationNodeWrapper.SafeArrayUnaccessData(safeArray);

                    *ret = safeArray;
                }

                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetBoundingRectangle(void* @this, Rect* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).BoundingRectangle;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetEmbeddedFragmentRoots(void* @this, SAFEARRAY** ret)
        {
            try
            {
                var arr = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).GetEmbeddedFragmentRoots();
                if (arr is not null)
                {
                    var bounds = new SAFEARRAYBOUND
                    {
                        cElements = (ulong)arr.Length,
                        lLbound = 0
                    };

                    var safeArray = AutomationNodeWrapper.SafeArrayCreate(AutomationNodeWrapper.VT_UNKNOWN, 1, &bounds);

                    void* pData;
                    AutomationNodeWrapper.SafeArrayAccessData(safeArray, &pData);
                    for (var i = 0; i < arr.Length; i++)
                    {
                        ((void**)pData)[i] = (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(arr[i], CreateComInterfaceFlags.None);
                    }
                    AutomationNodeWrapper.SafeArrayUnaccessData(safeArray);

                    *ret = safeArray;
                }
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int SetFocus(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).SetFocus();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetFragmentRoot(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).FragmentRoot;
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int ElementProviderFromPoint(void* @this, double x, double y, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).ElementProviderFromPoint(x, y);
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetFocus(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).GetFocus();
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IRawElementProviderFragmentRootNativeWrapper : IRawElementProviderFragmentRoot
    {
        public static IRawElementProviderFragment? ElementProviderFromPoint(void* @this, double x, double y)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 9)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragment?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        public static IRawElementProviderFragment? GetFocus(void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 10)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragment?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        IRawElementProviderFragment? IRawElementProviderFragment.Navigate(NavigateDirection direction) => IRawElementProviderFragmentNativeWrapper.Navigate((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst, direction);

        int[]? IRawElementProviderFragment.GetRuntimeId() => IRawElementProviderFragmentNativeWrapper.GetRuntimeId(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);

        Rect IRawElementProviderFragment.BoundingRectangle => IRawElementProviderFragmentNativeWrapper.GetBoundingRectangle(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);

        IRawElementProviderSimple[]? IRawElementProviderFragment.GetEmbeddedFragmentRoots() => IRawElementProviderFragmentNativeWrapper.GetEmbeddedFragmentRoots((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);

        void IRawElementProviderFragment.SetFocus() => IRawElementProviderFragmentNativeWrapper.SetFocus(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);

        IRawElementProviderFragmentRoot? IRawElementProviderFragment.FragmentRoot => IRawElementProviderFragmentNativeWrapper.GetFragmentRoot((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.ElementProviderFromPoint(double x, double y) => ElementProviderFromPoint(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst, x, y);

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.GetFocus() => GetFocus(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);
    }
#endif
}
