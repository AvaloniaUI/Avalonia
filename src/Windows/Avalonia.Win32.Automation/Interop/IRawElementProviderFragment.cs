using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Guid("670c3006-bf4c-428b-8534-e1848f645122")]
internal enum NavigateDirection
{
    Parent,
    NextSibling,
    PreviousSibling,
    FirstChild,
    LastChild,
}

#if NET8_0_OR_GREATER
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
#else
[ComImport()]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
[Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
internal partial interface IRawElementProviderFragment
{
    IRawElementProviderFragment? Navigate(NavigateDirection direction);
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<int>))]
#endif
    int[]? GetRuntimeId();
    Rect GetBoundingRectangle();
#if NET8_0_OR_GREATER
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
    IRawElementProviderSimple[]? GetEmbeddedFragmentRoots();
    void SetFocus();
    IRawElementProviderFragmentRoot? GetFragmentRoot();
}
