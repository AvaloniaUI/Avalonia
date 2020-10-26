using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("bd52d3c7-f990-4c52-9ae3-5c377e9eb772")]
    public enum ScrollAmount
    {
        LargeDecrement,
        SmallDecrement,
        NoAmount,
        LargeIncrement,
        SmallIncrement,
    }

    [ComVisible(true)]
    [Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IScrollProvider
    {
        void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
        double HorizontalScrollPercent { get; }
        double VerticalScrollPercent { get; }
        double HorizontalViewSize { get; }
        double VerticalViewSize { get; }
        bool HorizontallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool VerticallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }
}
