using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Automation.Provider;

namespace Avalonia.Win32.Automation.Interop;

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
internal partial interface IScrollProvider
{
    void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
    void SetScrollPercent(double horizontalPercent, double verticalPercent);
    double GetHorizontalScrollPercent();
    double GetVerticalScrollPercent();
    double GetHorizontalViewSize();
    double GetVerticalViewSize();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetHorizontallyScrollable();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetVerticallyScrollable();
}
