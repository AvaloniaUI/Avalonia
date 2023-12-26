using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISelectionProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("fb8b03af-3bdf-48d4-bd36-1a65793be168");
        public const int VtblSize = 3 + 3;
#endif
        IRawElementProviderSimple[] GetSelection();
        bool CanSelectMultiple { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool IsSelectionRequired { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class ISelectionProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int GetSelection(void* @this, SAFEARRAY** ret)
        {
            try
            {
                var arr = ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionProvider>((ComWrappers.ComInterfaceDispatch*)@this).GetSelection();

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
        public static int GetCanSelectMultiple(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionProvider>((ComWrappers.ComInterfaceDispatch*)@this).CanSelectMultiple;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetIsSelectionRequired(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionProvider>((ComWrappers.ComInterfaceDispatch*)@this).IsSelectionRequired;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface ISelectionProviderNativeWrapper : ISelectionProvider
    {
        public static IRawElementProviderSimple[] GetSelection(AutomationNodeWrapper container, void* @this)
        {
            SAFEARRAY ret;
            int hr = ((delegate* unmanaged<void*, SAFEARRAY*, int>)(*(*(void***)@this + 3)))(@this, &ret);

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

        public static bool GetCanSelectMultiple(void* @this) => AutomationNodeWrapper.InvokeAndGetBool(@this, 4);

        public static bool GetIsSelectionRequired(void* @this) => AutomationNodeWrapper.InvokeAndGetBool(@this, 5);

        IRawElementProviderSimple[] ISelectionProvider.GetSelection() => GetSelection((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).ISelectionProviderInst);

        bool ISelectionProvider.CanSelectMultiple => GetCanSelectMultiple(((AutomationNodeWrapper)this).ISelectionProviderInst);

        bool ISelectionProvider.IsSelectionRequired => GetIsSelectionRequired(((AutomationNodeWrapper)this).ISelectionProviderInst);
    }
#endif
}
