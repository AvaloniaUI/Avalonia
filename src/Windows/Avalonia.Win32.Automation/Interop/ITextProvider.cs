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

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("3589c92c-63f3-4367-99bb-ada653b77cf2")]
internal partial interface ITextProvider
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetSelection();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetVisibleRanges();
    ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);

    ITextRangeProvider RangeFromPoint(double X, double Y);

    ITextRangeProvider GetDocumentRange();
    SupportedTextSelection GetSupportedTextSelection();
}

// ITextProvider2 : public ITextProvider in UIAutomationCore.h. Declared flattened - the base slots
// repeated first, in vtable order - because the interop generator's derived-interface support differs
// across this project's target frameworks (SYSLIB1090 on net8.0); the resulting vtable is identical.
[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("0dc5e6ed-3e16-4bf1-8f9a-a979878bc195")]
internal partial interface ITextProvider2
{
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetSelection();
    [return: MarshalUsing(typeof(SafeArrayMarshaller<ITextRangeProvider>))]
    ITextRangeProvider[] GetVisibleRanges();
    ITextRangeProvider RangeFromChild(IRawElementProviderSimple childElement);

    ITextRangeProvider RangeFromPoint(double X, double Y);

    ITextRangeProvider GetDocumentRange();
    SupportedTextSelection GetSupportedTextSelection();

    ITextRangeProvider? RangeFromAnnotation(IRawElementProviderSimple? annotationElement);
    ITextRangeProvider? GetCaretRange([MarshalAs(UnmanagedType.Bool)] out bool isActive);
}
