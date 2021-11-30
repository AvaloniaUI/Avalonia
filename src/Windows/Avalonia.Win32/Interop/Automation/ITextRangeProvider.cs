using System.Runtime.InteropServices;

#nullable enable

namespace Avalonia.Win32.Interop.Automation
{
    public enum TextPatternRangeEndpoint
    {
        Start = 0,
        End = 1,
    }

    public enum TextUnit
    {
        Character = 0,
        Format = 1,
        Word = 2,
        Line = 3,
        Paragraph = 4,
        Page = 5,
        Document = 6,
    }

    public enum TextPatternAttribute
    {
        AnimationStyleAttributeId = 40000,
        BackgroundColorAttributeId = 40001,
        BulletStyleAttributeId = 40002,
        CapStyleAttributeId = 40003,
        CultureAttributeId = 40004,
        FontNameAttributeId = 40005,
        FontSizeAttributeId = 40006,
        FontWeightAttributeId = 40007,
        ForegroundColorAttributeId = 40008,
        HorizontalTextAlignmentAttributeId = 40009,
        IndentationFirstLineAttributeId = 40010,
        IndentationLeadingAttributeId = 40011,
        IndentationTrailingAttributeId = 40012,
        IsHiddenAttributeId = 40013,
        IsItalicAttributeId = 40014,
        IsReadOnlyAttributeId = 40015,
        IsSubscriptAttributeId = 40016,
        IsSuperscriptAttributeId = 40017,
        MarginBottomAttributeId = 40018,
        MarginLeadingAttributeId = 40019,
        MarginTopAttributeId = 40020,
        MarginTrailingAttributeId = 40021,
        OutlineStylesAttributeId = 40022,
        OverlineColorAttributeId = 40023,
        OverlineStyleAttributeId = 40024,
        StrikethroughColorAttributeId = 40025,
        StrikethroughStyleAttributeId = 40026,
        TabsAttributeId = 40027,
        TextFlowDirectionsAttributeId = 40028,
        UnderlineColorAttributeId = 40029,
        UnderlineStyleAttributeId = 40030,
        AnnotationTypesAttributeId = 40031,
        AnnotationObjectsAttributeId = 40032,
        StyleNameAttributeId = 40033,
        StyleIdAttributeId = 40034,
        LinkAttributeId = 40035,
        IsActiveAttributeId = 40036,
        SelectionActiveEndAttributeId = 40037,
        CaretPositionAttributeId = 40038,
        CaretBidiModeAttributeId = 40039,
        LineSpacingAttributeId = 40040,
        BeforeParagraphSpacingAttributeId = 40041,
        AfterParagraphSpacingAttributeId = 40042,
        SayAsInterpretAsAttributeId = 40043,
    }

    [ComVisible(true)]
    [Guid("5347ad7b-c355-46f8-aff5-909033582f63")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface ITextRangeProvider
    {
        ITextRangeProvider Clone();
        [return: MarshalAs(UnmanagedType.Bool)]
        bool Compare(ITextRangeProvider range);
        int CompareEndpoints(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void ExpandToEnclosingUnit(TextUnit unit);
        ITextRangeProvider? FindAttribute(TextPatternAttribute attribute, object? value, [MarshalAs(UnmanagedType.Bool)] bool backward);
        ITextRangeProvider? FindText(string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);
        object? GetAttributeValue(TextPatternAttribute attribute);
        double[] GetBoundingRectangles();
        IRawElementProviderSimple GetEnclosingElement();
        string GetText(int maxLength);
        int Move(TextUnit unit, int count);
        int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);
        void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, ITextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
        void Select();
        void AddToSelection();
        void RemoveFromSelection();
        void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
        IRawElementProviderSimple[] GetChildren();
    }
}
