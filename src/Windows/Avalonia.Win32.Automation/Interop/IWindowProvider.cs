using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Guid("fdc8f176-aed2-477a-8c89-ea04cc5f278d")]
internal enum WindowVisualState
{
    Normal,
    Maximized,
    Minimized
}

[Guid("65101cc7-7904-408e-87a7-8c6dbd83a18b")]
internal enum WindowInteractionState
{
    Running,
    Closing,
    ReadyForUserInteraction,
    BlockedByModalWindow,
    NotResponding
}
#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("987df77b-db06-4d77-8f8a-86a9c3bb90b9")]
internal partial interface IWindowProvider
{
    void SetVisualState(WindowVisualState state);
    void Close();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool WaitForInputIdle(int milliseconds);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetMaximizable();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetMinimizable();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsModal();

    WindowVisualState GetVisualState();
    WindowInteractionState GetInteractionState();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsTopmost();
}
