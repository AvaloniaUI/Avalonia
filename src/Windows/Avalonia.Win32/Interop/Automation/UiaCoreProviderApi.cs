using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("d8e55844-7043-4edc-979d-593cc6b4775e")]
    internal enum AsyncContentLoadedState
    {
        Beginning,
        Progress,
        Completed,
    }

    [ComVisible(true)]
    [Guid("e4cfef41-071d-472c-a65c-c14f59ea81eb")]
    internal enum StructureChangeType
    {
        ChildAdded,
        ChildRemoved,
        ChildrenInvalidated,
        ChildrenBulkAdded,
        ChildrenBulkRemoved,
        ChildrenReordered,
    }
    internal static class UiaCoreProviderApi
    {
        private const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);

        internal static void UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int propertyId, object oldValue, object newValue)
        {
            CheckError(RawUiaRaiseAutomationPropertyChangedEvent(provider, propertyId, oldValue, newValue));
        }

        internal static void UiaRaiseAutomationEvent(IRawElementProviderSimple provider, int eventId)
        {
            CheckError(RawUiaRaiseAutomationEvent(provider, eventId));
        }

        internal static void UiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId)
        {
            CheckError(RawUiaRaiseStructureChangedEvent(provider, structureChangeType, runtimeId, runtimeId == null ? 0 : runtimeId.Length));
        }

        internal static void UiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete)
        {
            CheckError(RawUiaRaiseAsyncContentLoadedEvent(provider, asyncContentLoadedState, PercentComplete));
        }

        internal static bool UiaClientsAreListening()
        {
            return RawUiaClientsAreListening();
        }

        private static void CheckError(int hr)
        {
            if (hr >= 0 || hr == UIA_E_ELEMENTNOTAVAILABLE)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
        }

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaReturnRawElementProvider", CharSet = CharSet.Unicode)]
        public static extern IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple el);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaHostProviderFromHwnd", CharSet = CharSet.Unicode)]
        public static extern int UiaHostProviderFromHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseAutomationPropertyChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple provider, int id, object oldValue, object newValue);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseAutomationEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAutomationEvent(IRawElementProviderSimple provider, int id);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseStructureChangedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseStructureChangedEvent(IRawElementProviderSimple provider, StructureChangeType structureChangeType, int[] runtimeId, int runtimeIdLen);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaRaiseAsyncContentLoadedEvent", CharSet = CharSet.Unicode)]
        private static extern int RawUiaRaiseAsyncContentLoadedEvent(IRawElementProviderSimple provider, AsyncContentLoadedState asyncContentLoadedState, double PercentComplete);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaClientsAreListening", CharSet = CharSet.Unicode)]
        private static extern bool RawUiaClientsAreListening();
    }
}
