using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation.Provider;

namespace Avalonia.Win32.Interop.Automation
{
#if NET8_0_OR_GREATER
    [GeneratedComInterface]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
    [Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    internal partial interface IScrollProvider
    {
        void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
        double HorizontalScrollPercent();
        double VerticalScrollPercent();
        double HorizontalViewSize();
        double VerticalViewSize();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool HorizontallyScrollable();

        [return: MarshalAs(UnmanagedType.Bool)]
        bool VerticallyScrollable();
    }
}
