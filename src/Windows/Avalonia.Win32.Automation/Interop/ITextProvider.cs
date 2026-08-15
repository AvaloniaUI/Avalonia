using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[Flags]
[Guid("3d9e3d8f-bfb0-484f-84ab-93ff4280cbc4")]
internal enum SupportedTextSelection
{
    None,
    Single,
    Multiple,
}

/// <summary>
/// Matches the native UIA <c>UiaPoint</c> struct (two packed doubles), which
/// <see cref="ITextProvider.RangeFromPoint"/> takes by value per the real COM ABI — unlike
/// <see cref="IRawElementProviderFragmentRoot.ElementProviderFromPoint"/>, which takes two
/// separate scalar doubles. Passing two scalar doubles here instead of this struct mismatches
/// the native calling convention and corrupts the call.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UiaPoint
{
    public double X;
    public double Y;
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("3589c92c-63f3-4367-99bb-ada653b77cf2")]
internal partial interface ITextProvider
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetSelection();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetVisibleRanges();
    ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);

    ITextRangeProvider RangeFromPoint(UiaPoint point);

    ITextRangeProvider GetDocumentRange();
    SupportedTextSelection GetSupportedTextSelection();
}
