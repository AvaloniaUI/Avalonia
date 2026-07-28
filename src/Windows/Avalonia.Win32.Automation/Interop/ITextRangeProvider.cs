using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

internal enum TextPatternRangeEndpoint
{
    Start = 0,
    End = 1,
}

internal enum TextUnit
{
    Character = 0,
    Format = 1,
    Word = 2,
    Line = 3,
    Paragraph = 4,
    Page = 5,
    Document = 6,
}

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("5347ad7b-c355-46f8-aff5-909033582f63")]
internal partial interface ITextRangeProvider
{
    ITextRangeProvider Clone();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool Compare(ITextRangeProvider range);

    int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange,
        TextPatternRangeEndpoint targetEndpoint);

    void ExpandToEnclosingUnit(TextUnit unit);

    ITextRangeProvider FindAttribute(int attribute,
        [MarshalUsing(typeof(ComVariantMarshaller))]
        object value, [MarshalAs(UnmanagedType.Bool)] bool backward);

    ITextRangeProvider FindText(
        [MarshalAs(UnmanagedType.BStr)] string text,
        [MarshalAs(UnmanagedType.Bool)] bool backward,
        [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);
    [return: MarshalUsing(typeof(ComVariantMarshaller))]
    object GetAttributeValue(int attribute);
    [return: MarshalUsing(typeof(SafeArrayMarshaller<double>))]
    double[] GetBoundingRectangles();
    IRawElementProviderSimple GetEnclosingElement();
    [return: MarshalAs(UnmanagedType.BStr)]
    string GetText(int maxLength);
    int Move(TextUnit unit, int count);
    int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

    void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange,
        TextPatternRangeEndpoint targetEndpoint);

    void Select();
    void AddToSelection();
    void RemoveFromSelection();
    void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
    [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
    IRawElementProviderSimple[] GetChildren();
}
