using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.DirectX;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.DComposition;

internal class NativeMethods
{
    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionAttachMouseDragToHwnd( /* _In_ */
        IntPtr visual, /* _In_ */ IntPtr hwnd, /* _In_ */ bool enable);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionAttachMouseWheelToHwnd( /* _In_ */
        IntPtr visual, /* _In_ */ IntPtr hwnd, /* _In_ */ bool enable);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionBoostCompositorClock( /* _In_ */ bool enable);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateDevice( /* _In_opt_ */
        IntPtr dxgiDevice, /* _In_ */ [MarshalAs(UnmanagedType.LPStruct)] Guid iid, /* _Outptr_ */
        out IntPtr dcompositionDevice);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateDevice2( /* _In_opt_ */
        IntPtr renderingDevice, /* _In_ */
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid, /* _Outptr_ */ out IntPtr dcompositionDevice);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateDevice3( /* _In_opt_ */
        IntPtr renderingDevice, /* _In_ */
        [MarshalAs(UnmanagedType.LPStruct)] Guid iid, /* _Outptr_ */ out IntPtr dcompositionDevice);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern UnmanagedMethods.HRESULT DCompositionCreateSurfaceHandle( /* _In_ */
        uint desiredAccess, /* optional(SECURITY_ATTRIBUTES) */ IntPtr securityAttributes, /* _Out_ */
        out IntPtr surfaceHandle);

    // [DllImport("dcomp", ExactSpelling = true)]
    // public static extern UnmanagedMethods.HRESULT DCompositionGetFrameId( /* _In_ */
    //     COMPOSITION_FRAME_ID_TYPE frameIdType, /* _Out_ */ out COMPOSITION_FRAME_ID frameId);
    //
    // [DllImport("dcomp", ExactSpelling = true)]
    // public static extern UnmanagedMethods.HRESULT DCompositionGetStatistics( /* _In_ */ ulong frameId, /* _Out_ */
    //     out tagCOMPOSITION_FRAME_STATS frameStats, /* _In_ */ uint targetIdCount, /* _Out_writes_opt_(targetCount) */
    //     [Out, MarshalAs(UnmanagedType.LPArray)] tagCOMPOSITION_TARGET_ID[] targetIds, /* optional(UINT) */
    //     IntPtr actualTargetIdCount);
    //
    // [DllImport("dcomp", ExactSpelling = true)]
    // public static extern UnmanagedMethods.HRESULT DCompositionGetTargetStatistics( /* _In_ */ ulong frameId, /* _In_ */
    //     ref tagCOMPOSITION_TARGET_ID targetId, /* _Out_ */ out tagCOMPOSITION_TARGET_STATS targetStats);

    [DllImport("dcomp", ExactSpelling = true)]
    public static extern uint
        DCompositionWaitForCompositorClock( /* _In_range_(0, DCOMPOSITION_MAX_WAITFORCOMPOSITORCLOCK_OBJECTS) */
            int count, /* _In_reads_opt_(count) */
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] IntPtr[] handles, /* _In_ */ uint timeoutInMs);
}
