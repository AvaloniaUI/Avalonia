using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Win32.Automation.Marshalling;

namespace Avalonia.Win32.Interop.Automation
{
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
#if NET8_0_OR_GREATER
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf8)]
#else
    [ComImport()]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
#endif
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
#if NET8_0_OR_GREATER
            [MarshalUsing(typeof(ComVariantMarshaller))]
#endif
            object value, [MarshalAs(UnmanagedType.Bool)] bool backward);

        ITextRangeProvider FindText(string text,
            [MarshalAs(UnmanagedType.Bool)] bool backward,
            [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);
#if NET8_0_OR_GREATER
        [return: MarshalUsing(typeof(ComVariantMarshaller))]
#endif
        object GetAttributeValue(int attribute);
#if NET8_0_OR_GREATER
        [return: MarshalUsing(typeof(SafeArrayMarshaller<double>))]
#endif
        IReadOnlyList<double> GetBoundingRectangles();
        IRawElementProviderSimple GetEnclosingElement();
        string GetText(int maxLength);
        int Move(TextUnit unit, int count);
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);

        void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange,
            TextPatternRangeEndpoint targetEndpoint);

        void Select();
        void AddToSelection();
        void RemoveFromSelection();
        void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
#if NET8_0_OR_GREATER
        [return: MarshalUsing(typeof(SafeArrayMarshaller<IRawElementProviderSimple>))]
#endif
        IReadOnlyList<IRawElementProviderSimple> GetChildren();
    }
}
