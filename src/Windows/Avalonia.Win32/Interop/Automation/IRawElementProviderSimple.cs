using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [Flags]
    public enum ProviderOptions
    {
        ClientSideProvider = 0x0001,
        ServerSideProvider = 0x0002,
        NonClientAreaProvider = 0x0004,
        OverrideProvider = 0x0008,
        ProviderOwnsSetFocus = 0x0010,
        UseComThreading = 0x0020
    }

    internal enum UiaPropertyId
    {
        RuntimeId = 30000,
        BoundingRectangle,
        ProcessId,
        ControlType,
        LocalizedControlType,
        Name,
        AcceleratorKey,
        AccessKey,
        HasKeyboardFocus,
        IsKeyboardFocusable,
        IsEnabled,
        AutomationId,
        ClassName,
        HelpText,
        ClickablePoint,
        Culture,
        IsControlElement,
        IsContentElement,
        LabeledBy,
        IsPassword,
        NativeWindowHandle,
        ItemType,
        IsOffscreen,
        Orientation,
        FrameworkId,
        IsRequiredForForm,
        ItemStatus,
        IsDockPatternAvailable,
        IsExpandCollapsePatternAvailable,
        IsGridItemPatternAvailable,
        IsGridPatternAvailable,
        IsInvokePatternAvailable,
        IsMultipleViewPatternAvailable,
        IsRangeValuePatternAvailable,
        IsScrollPatternAvailable,
        IsScrollItemPatternAvailable,
        IsSelectionItemPatternAvailable,
        IsSelectionPatternAvailable,
        IsTablePatternAvailable,
        IsTableItemPatternAvailable,
        IsTextPatternAvailable,
        IsTogglePatternAvailable,
        IsTransformPatternAvailable,
        IsValuePatternAvailable,
        IsWindowPatternAvailable,
        ValueValue,
        ValueIsReadOnly,
        RangeValueValue,
        RangeValueIsReadOnly,
        RangeValueMinimum,
        RangeValueMaximum,
        RangeValueLargeChange,
        RangeValueSmallChange,
        ScrollHorizontalScrollPercent,
        ScrollHorizontalViewSize,
        ScrollVerticalScrollPercent,
        ScrollVerticalViewSize,
        ScrollHorizontallyScrollable,
        ScrollVerticallyScrollable,
        SelectionSelection,
        SelectionCanSelectMultiple,
        SelectionIsSelectionRequired,
        GridRowCount,
        GridColumnCount,
        GridItemRow,
        GridItemColumn,
        GridItemRowSpan,
        GridItemColumnSpan,
        GridItemContainingGrid,
        DockDockPosition,
        ExpandCollapseExpandCollapseState,
        MultipleViewCurrentView,
        MultipleViewSupportedViews,
        WindowCanMaximize,
        WindowCanMinimize,
        WindowWindowVisualState,
        WindowWindowInteractionState,
        WindowIsModal,
        WindowIsTopmost,
        SelectionItemIsSelected,
        SelectionItemSelectionContainer,
        TableRowHeaders,
        TableColumnHeaders,
        TableRowOrColumnMajor,
        TableItemRowHeaderItems,
        TableItemColumnHeaderItems,
        ToggleToggleState,
        TransformCanMove,
        TransformCanResize,
        TransformCanRotate,
        IsLegacyIAccessiblePatternAvailable,
        LegacyIAccessibleChildId,
        LegacyIAccessibleName,
        LegacyIAccessibleValue,
        LegacyIAccessibleDescription,
        LegacyIAccessibleRole,
        LegacyIAccessibleState,
        LegacyIAccessibleHelp,
        LegacyIAccessibleKeyboardShortcut,
        LegacyIAccessibleSelection,
        LegacyIAccessibleDefaultAction,
        AriaRole,
        AriaProperties,
        IsDataValidForForm,
        ControllerFor,
        DescribedBy,
        FlowsTo,
        ProviderDescription,
        IsItemContainerPatternAvailable,
        IsVirtualizedItemPatternAvailable,
        IsSynchronizedInputPatternAvailable,
        OptimizeForVisualContent,
        IsObjectModelPatternAvailable,
        AnnotationAnnotationTypeId,
        AnnotationAnnotationTypeName,
        AnnotationAuthor,
        AnnotationDateTime,
        AnnotationTarget,
        IsAnnotationPatternAvailable,
        IsTextPattern2Available,
        StylesStyleId,
        StylesStyleName,
        StylesFillColor,
        StylesFillPatternStyle,
        StylesShape,
        StylesFillPatternColor,
        StylesExtendedProperties,
        IsStylesPatternAvailable,
        IsSpreadsheetPatternAvailable,
        SpreadsheetItemFormula,
        SpreadsheetItemAnnotationObjects,
        SpreadsheetItemAnnotationTypes,
        IsSpreadsheetItemPatternAvailable,
        Transform2CanZoom,
        IsTransformPattern2Available,
        LiveSetting,
        IsTextChildPatternAvailable,
        IsDragPatternAvailable,
        DragIsGrabbed,
        DragDropEffect,
        DragDropEffects,
        IsDropTargetPatternAvailable,
        DropTargetDropTargetEffect,
        DropTargetDropTargetEffects,
        DragGrabbedItems,
        Transform2ZoomLevel,
        Transform2ZoomMinimum,
        Transform2ZoomMaximum,
        FlowsFrom,
        IsTextEditPatternAvailable,
        IsPeripheral,
        IsCustomNavigationPatternAvailable,
        PositionInSet,
        SizeOfSet,
        Level,
        AnnotationTypes,
        AnnotationObjects,
        LandmarkType,
        LocalizedLandmarkType,
        FullDescription,
        FillColor,
        OutlineColor,
        FillType,
        VisualEffects,
        OutlineThickness,
        CenterPoint,
        Rotatation,
        Size
    }

    internal enum UiaPatternId
    {
        Invoke = 10000,
        Selection,
        Value,
        RangeValue,
        Scroll,
        ExpandCollapse,
        Grid,
        GridItem,
        MultipleView,
        Window,
        SelectionItem,
        Dock,
        Table,
        TableItem,
        Text,
        Toggle,
        Transform,
        ScrollItem,
        LegacyIAccessible,
        ItemContainer,
        VirtualizedItem,
        SynchronizedInput,
        ObjectModel,
        Annotation,
        Text2,
        Styles,
        Spreadsheet,
        SpreadsheetItem,
        Transform2,
        TextChild,
        Drag,
        DropTarget,
        TextEdit,
        CustomNavigation
    };

    internal enum UiaControlTypeId
    {
        Button = 50000,
        Calendar,
        CheckBox,
        ComboBox,
        Edit,
        Hyperlink,
        Image,
        ListItem,
        List,
        Menu,
        MenuBar,
        MenuItem,
        ProgressBar,
        RadioButton,
        ScrollBar,
        Slider,
        Spinner,
        StatusBar,
        Tab,
        TabItem,
        Text,
        ToolBar,
        ToolTip,
        Tree,
        TreeItem,
        Custom,
        Group,
        Thumb,
        DataGrid,
        DataItem,
        Document,
        SplitButton,
        Window,
        Pane,
        Header,
        HeaderItem,
        Table,
        TitleBar,
        Separator,
        SemanticZoom,
        AppBar
    };

    [ComVisible(true)]
    [Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRawElementProviderSimple
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("d6dd68d1-86fd-4332-8666-9abedea2d24c");
        public const int VtblSize = 3 + 4;
#endif
        ProviderOptions ProviderOptions { get; }
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object? GetPatternProvider(int patternId);
        object? GetPropertyValue(int propertyId);
        IRawElementProviderSimple? HostRawElementProvider { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRawElementProviderSimpleManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int GetProviderOptions(void* @this, ProviderOptions* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple>((ComWrappers.ComInterfaceDispatch*)@this).ProviderOptions;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetPatternProvider(void* @this, int patternId, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple>((ComWrappers.ComInterfaceDispatch*)@this).GetPatternProvider(patternId);
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetPropertyValue(void* @this, int propertyId, VARIANT* ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple>((ComWrappers.ComInterfaceDispatch*)@this).GetPropertyValue(propertyId);
                var variant = obj switch
                {
                    bool b => new VARIANT { vt = (ushort)VarEnum.VT_BOOL, ptr0 = (IntPtr)(b ? 1 : 0) },
                    sbyte i1 => new VARIANT { vt = (ushort)VarEnum.VT_I1, ptr0 = Unsafe.As<sbyte, IntPtr>(ref i1) },
                    short i2 => new VARIANT { vt = (ushort)VarEnum.VT_I2, ptr0 = Unsafe.As<short, IntPtr>(ref i2) },
                    int i4 => new VARIANT { vt = (ushort)VarEnum.VT_I4, ptr0 = Unsafe.As<int, IntPtr>(ref i4) },
                    long i8 => new VARIANT { vt = (ushort)VarEnum.VT_I8, ptr0 = Unsafe.As<long, IntPtr>(ref i8) },
                    byte u1 => new VARIANT { vt = (ushort)VarEnum.VT_UI1, ptr0 = Unsafe.As<byte, IntPtr>(ref u1) },
                    ushort u2 => new VARIANT { vt = (ushort)VarEnum.VT_UI2, ptr0 = Unsafe.As<ushort, IntPtr>(ref u2) },
                    uint u4 => new VARIANT { vt = (ushort)VarEnum.VT_UI4, ptr0 = Unsafe.As<uint, IntPtr>(ref u4) },
                    ulong u8 => new VARIANT { vt = (ushort)VarEnum.VT_UI8, ptr0 = Unsafe.As<ulong, IntPtr>(ref u8) },
                    float r4 => new VARIANT { vt = (ushort)VarEnum.VT_R4, ptr0 = Unsafe.As<float, IntPtr>(ref r4) },
                    double r8 => new VARIANT { vt = (ushort)VarEnum.VT_R8, ptr0 = Unsafe.As<double, IntPtr>(ref r8) },
                    decimal m => new VARIANT { vt = (ushort)VarEnum.VT_DECIMAL, data = m },
                    string s => new VARIANT { vt = (ushort)VarEnum.VT_BSTR, ptr0 = Marshal.StringToBSTR(s) },
                    null => new VARIANT { vt = (ushort)VarEnum.VT_NULL },
                    _ => new VARIANT { vt = (ushort)VarEnum.VT_UNKNOWN, ptr0 = AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None) },
                };

                *ret = variant;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetHostRawElementProvider(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple>((ComWrappers.ComInterfaceDispatch*)@this).HostRawElementProvider;
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
    internal unsafe interface IRawElementProviderSimpleNativeWrapper : IRawElementProviderSimple
    {
        public static ProviderOptions GetProviderOptions(void* @this)
        {
            ProviderOptions ret;
            int hr = ((delegate* unmanaged<void*, ProviderOptions*, int>)(*(*(void***)@this + 3)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret;
        }

        public static object? GetPatternProvider(AutomationNodeWrapper container, void* @this, int patternId)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, int, void**, int>)(*(*(void***)@this + 4)))(@this, patternId, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret == null ? null : (IRawElementProviderSimple)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        public static object? GetPropertyValue(AutomationNodeWrapper container, void* @this, int propertyId)
        {
            VARIANT ret;
            int hr = ((delegate* unmanaged<void*, int, VARIANT*, int>)(*(*(void***)@this + 5)))(@this, propertyId, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (VarEnum)ret.vt switch
            {
                VarEnum.VT_BOOL => Unsafe.As<IntPtr, bool>(ref ret.ptr0),
                VarEnum.VT_I1 => Unsafe.As<IntPtr, sbyte>(ref ret.ptr0),
                VarEnum.VT_I2 => Unsafe.As<IntPtr, short>(ref ret.ptr0),
                VarEnum.VT_I4 or VarEnum.VT_INT => Unsafe.As<IntPtr, int>(ref ret.ptr0),
                VarEnum.VT_I8 => Unsafe.As<IntPtr, long>(ref ret.ptr0),
                VarEnum.VT_UI1 => Unsafe.As<IntPtr, byte>(ref ret.ptr0),
                VarEnum.VT_UI2 => Unsafe.As<IntPtr, ushort>(ref ret.ptr0),
                VarEnum.VT_UI4 or VarEnum.VT_UINT => Unsafe.As<IntPtr, uint>(ref ret.ptr0),
                VarEnum.VT_UI8 => Unsafe.As<IntPtr, ulong>(ref ret.ptr0),
                VarEnum.VT_R4 => Unsafe.As<IntPtr, float>(ref ret.ptr0),
                VarEnum.VT_R8 => Unsafe.As<IntPtr, double>(ref ret.ptr0),
                VarEnum.VT_DECIMAL => ret.data,
                VarEnum.VT_BSTR => Marshal.PtrToStringBSTR(ret.ptr0),
                VarEnum.VT_NULL => null,
                VarEnum.VT_UNKNOWN => AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance(ret.ptr0, CreateObjectFlags.None),
                _ => throw new NotSupportedException()
            };
        }

        public static IRawElementProviderSimple? GetHostRawElementProvider(AutomationNodeWrapper container, void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 6)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret == null ? null : (IRawElementProviderSimple)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        ProviderOptions IRawElementProviderSimple.ProviderOptions => GetProviderOptions(((AutomationNodeWrapper)this).IRawElementProviderSimpleInst);

        object? IRawElementProviderSimple.GetPatternProvider(int patternId) => GetPatternProvider((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimpleInst, patternId);

        object? IRawElementProviderSimple.GetPropertyValue(int propertyId) => GetPropertyValue((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimpleInst, propertyId);

        IRawElementProviderSimple? IRawElementProviderSimple.HostRawElementProvider => GetHostRawElementProvider((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimpleInst);
    }
#endif
}
