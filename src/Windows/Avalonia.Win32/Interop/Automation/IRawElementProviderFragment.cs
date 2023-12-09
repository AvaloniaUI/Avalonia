using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("670c3006-bf4c-428b-8534-e1848f645122")]
    public enum NavigateDirection
    {
        Parent,
        NextSibling,
        PreviousSibling,
        FirstChild,
        LastChild,
    }

    // NOTE: This interface needs to be public otherwise Navigate is never called. I have no idea
    // why given that IRawElementProviderSimple and IRawElementProviderFragmentRoot seem to get
    // called fine when they're internal, but I lost a couple of days to this.
    [ComVisible(true)]
    [Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRawElementProviderFragment
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("f7063da8-8359-439c-9297-bbc5299a7d87");
        public const int VtblSize = 3 + 6;
#endif
        IRawElementProviderFragment? Navigate(NavigateDirection direction);
        int[]? GetRuntimeId();
        Rect BoundingRectangle { get; }
        IRawElementProviderSimple[]? GetEmbeddedFragmentRoots();
        void SetFocus();
        IRawElementProviderFragmentRoot? FragmentRoot { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRawElementProviderFragmentManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Navigate(void* @this, NavigateDirection direction, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).Navigate(direction);
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
                var arr = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).GetRuntimeId();
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
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).BoundingRectangle;
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
                var arr = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).GetEmbeddedFragmentRoots();
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
                ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).SetFocus();
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
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragment>((ComWrappers.ComInterfaceDispatch*)@this).FragmentRoot;
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
    internal unsafe interface IRawElementProviderFragmentNativeWrapper : IRawElementProviderFragment
    {
        public static IRawElementProviderFragment? Navigate(AutomationNodeWrapper container, void* @this, NavigateDirection direction)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, NavigateDirection, void**, int>)(*(*(void***)@this + 3)))(@this, direction, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragment)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        public static int[]? GetRuntimeId(void* @this)
        {
            SAFEARRAY ret;
            int hr = ((delegate* unmanaged<void*, SAFEARRAY*, int>)(*(*(void***)@this + 4)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var arr = new int[ret.rgsabound->cElements];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = ((int*)ret.pvData)[i];
            }

            AutomationNodeWrapper.SafeArrayDestroy(&ret);
            return arr;
        }

        public static Rect GetBoundingRectangle(void* @this) => AutomationNodeWrapper.InvokeAndGet<Rect>(@this, 5);

        public static IRawElementProviderSimple[]? GetEmbeddedFragmentRoots(AutomationNodeWrapper container, void* @this)
        {
            SAFEARRAY ret;
            int hr = ((delegate* unmanaged<void*, SAFEARRAY*, int>)(*(*(void***)@this + 6)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            var arr = new IRawElementProviderSimple[ret.rgsabound->cElements];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (IRawElementProviderSimple)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)((void**)ret.pvData)[i], CreateObjectFlags.None);
            }

            AutomationNodeWrapper.SafeArrayDestroy(&ret);
            return arr;
        }

        public static void SetFocus(void* @this) => AutomationNodeWrapper.Invoke(@this, 7);

        public static IRawElementProviderFragmentRoot? GetFragmentRoot(AutomationNodeWrapper container, void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 8)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragmentRoot?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        IRawElementProviderFragment? IRawElementProviderFragment.Navigate(NavigateDirection direction) => Navigate((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentInst, direction);

        int[]? IRawElementProviderFragment.GetRuntimeId() => GetRuntimeId(((AutomationNodeWrapper)this).IRawElementProviderFragmentInst);

        Rect IRawElementProviderFragment.BoundingRectangle => GetBoundingRectangle(((AutomationNodeWrapper)this).IRawElementProviderFragmentInst);

        IRawElementProviderSimple[]? IRawElementProviderFragment.GetEmbeddedFragmentRoots() => GetEmbeddedFragmentRoots((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentInst);

        void IRawElementProviderFragment.SetFocus() => SetFocus(((AutomationNodeWrapper)this).IRawElementProviderFragmentInst);

        IRawElementProviderFragmentRoot? IRawElementProviderFragment.FragmentRoot => GetFragmentRoot((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderFragmentInst);
    }
#endif
}
