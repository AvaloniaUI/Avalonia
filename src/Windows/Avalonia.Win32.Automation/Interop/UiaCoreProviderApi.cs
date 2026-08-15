using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop
{
    [Guid("d8e55844-7043-4edc-979d-593cc6b4775e")]
    internal enum AsyncContentLoadedState
    {
        Beginning,
        Progress,
        Completed,
    }

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

    internal enum UiaEventId
    {
        ToolTipOpened = 20000,
        ToolTipClosed,
        StructureChanged,
        MenuOpened,
        AutomationPropertyChanged,
        AutomationFocusChanged,
        AsyncContentLoaded,
        MenuClosed,
        LayoutInvalidated,
        Invoke_Invoked,
        SelectionItem_ElementAddedToSelection,
        SelectionItem_ElementRemovedFromSelection,
        SelectionItem_ElementSelected,
        Selection_Invalidated,
        Text_TextSelectionChanged,
        Text_TextChanged,
        Window_WindowOpened,
        Window_WindowClosed,
        MenuModeStart,
        MenuModeEnd,
        InputReachedTarget,
        InputReachedOtherElement,
        InputDiscarded,
        SystemAlert,
        LiveRegionChanged,
        HostedFragmentRootsInvalidated,
        Drag_DragStart,
        Drag_DragCancel,
        Drag_DragComplete,
        DropTarget_DragEnter,
        DropTarget_DragLeave,
        DropTarget_Dropped,
        TextEdit_TextChanged,
        TextEdit_ConversionTargetChanged,
        Changes
    };

    internal static partial class UiaCoreProviderApi
    {
        public const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool UiaClientsAreListening();
        
        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr UiaReturnRawElementProvider(IntPtr hwnd, IntPtr wParam, IntPtr lParam, IRawElementProviderSimple? el);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaHostProviderFromHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple provider);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaRaiseAutomationEvent(IRawElementProviderSimple? provider, int id);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaRaiseAutomationPropertyChangedEvent(IRawElementProviderSimple? provider, int id, [MarshalUsing(typeof(ComVariantMarshaller))] object? oldValue, [MarshalUsing(typeof(ComVariantMarshaller))] object? newValue);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaRaiseStructureChangedEvent(IRawElementProviderSimple? provider, StructureChangeType structureChangeType, int[]? runtimeId, int runtimeIdLen);

        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaDisconnectProvider(IRawElementProviderSimple? provider);

        /// <summary>
        /// Gets the reserved COM value UI Automation clients (e.g. NVDA) expect back from
        /// <c>ITextRangeProvider.GetAttributeValue</c> for an attribute the control doesn't
        /// support. Returning a plain sentinel like <c>-1</c> instead is read by at least NVDA as
        /// a real (truthy) attribute value rather than "not supported" — e.g. for the hyperlink
        /// attribute, that gets a plain TextBox announced as a link.
        /// </summary>
        [LibraryImport("UIAutomationCore.dll", StringMarshalling = StringMarshalling.Utf8)]
        public static partial int UiaGetReservedNotSupportedValue(out IntPtr punkNotSupportedValue);
    }
}
