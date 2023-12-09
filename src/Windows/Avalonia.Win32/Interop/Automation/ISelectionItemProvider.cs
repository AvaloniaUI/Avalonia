using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("2acad808-b2d4-452d-a407-91ff1ad167b2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ISelectionItemProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("2acad808-b2d4-452d-a407-91ff1ad167b2");
        public const int VtblSize = 3 + 5;
#endif
        void Select();
        void AddToSelection();
        void RemoveFromSelection();
        bool IsSelected { [return: MarshalAs(UnmanagedType.Bool)] get; }
        IRawElementProviderSimple? SelectionContainer { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class ISelectionItemProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Select(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).Select();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int AddToSelection(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).AddToSelection();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int RemoveFromSelection(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).RemoveFromSelection();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetIsSelected(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).IsSelected;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetSelectionContainer(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<ISelectionItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).SelectionContainer;
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
    internal unsafe interface ISelectionItemProviderNativeWrapper : ISelectionItemProvider
    {
        public static void Select(void* @this) => AutomationNodeWrapper.Invoke(@this, 3);

        public static void AddToSelection(void* @this) => AutomationNodeWrapper.Invoke(@this, 4);

        public static void RemoveFromSelection(void* @this) => AutomationNodeWrapper.Invoke(@this, 5);

        public static bool GetIsSelected(void* @this) => AutomationNodeWrapper.InvokeAndGet<bool>(@this, 6);

        public static IRawElementProviderSimple? GetSelectionContainer(AutomationNodeWrapper container, void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 7)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderSimple?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        void ISelectionItemProvider.Select() => Select(((AutomationNodeWrapper)this).ISelectionItemProviderInst);

        void ISelectionItemProvider.AddToSelection() => AddToSelection(((AutomationNodeWrapper)this).ISelectionItemProviderInst);

        void ISelectionItemProvider.RemoveFromSelection() => RemoveFromSelection(((AutomationNodeWrapper)this).ISelectionItemProviderInst);

        bool ISelectionItemProvider.IsSelected => GetIsSelected(((AutomationNodeWrapper)this).ISelectionItemProviderInst);

        IRawElementProviderSimple? ISelectionItemProvider.SelectionContainer => GetSelectionContainer((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).ISelectionItemProviderInst);
    }
#endif
}

