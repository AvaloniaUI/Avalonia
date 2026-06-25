using System;
using System.Runtime.InteropServices.Marshalling;
using AAP = Avalonia.Automation.Provider;
using AvTextUnit = Avalonia.Input.TextInput.TextUnit;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    /// <summary>
    /// Exposes the cross-platform <see cref="AAP.ITextRangeProvider"/> cursor as a UIA TextPattern
    /// range. Navigation and text extraction forward to the cross-platform range; geometry, attributes,
    /// selection-write, find and children are deferred (benign defaults) to a later pass.
    /// </summary>
    [GeneratedComClass]
    internal partial class AutomationTextRangeProvider : UIA.ITextRangeProvider
    {
        private readonly AutomationNode _owner;
        private readonly AAP.ITextRangeProvider _range;

        public AutomationTextRangeProvider(AutomationNode owner, AAP.ITextRangeProvider range)
        {
            _owner = owner;
            _range = range;
        }

        public UIA.ITextRangeProvider Clone() => new AutomationTextRangeProvider(_owner, _range.Clone());

        public bool Compare(UIA.ITextRangeProvider range) => _range.Compare(Unwrap(range));

        public int CompareEndpoints(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange,
            UIA.TextPatternRangeEndpoint targetEndpoint)
            => _range.CompareEndpoints(Map(endpoint), Unwrap(targetRange), Map(targetEndpoint));

        public void ExpandToEnclosingUnit(UIA.TextUnit unit) => _range.ExpandToEnclosingUnit(Map(unit));

        // Deferred: attribute search.
        public UIA.ITextRangeProvider FindAttribute(int attribute, object value, bool backward) => null!;

        // Deferred: text search.
        public UIA.ITextRangeProvider FindText(string text, bool backward, bool ignoreCase) => null!;

        // Deferred: attribute values.
        public object GetAttributeValue(int attribute) => null!;

        // Deferred: bounding rectangles (needs layout/screen coordinates).
        public double[] GetBoundingRectangles() => Array.Empty<double>();

        public UIA.IRawElementProviderSimple GetEnclosingElement() => _owner;

        public string GetText(int maxLength) => _range.GetText(maxLength);

        public int Move(UIA.TextUnit unit, int count) => _range.Move(Map(unit), count);

        public int MoveEndpointByUnit(UIA.TextPatternRangeEndpoint endpoint, UIA.TextUnit unit, int count)
            => _range.MoveEndpointByUnit(Map(endpoint), Map(unit), count);

        public void MoveEndpointByRange(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange,
            UIA.TextPatternRangeEndpoint targetEndpoint)
            => _range.MoveEndpointByRange(Map(endpoint), Unwrap(targetRange), Map(targetEndpoint));

        // Deferred: selection write.
        public void Select() { }
        public void AddToSelection() { }
        public void RemoveFromSelection() { }

        // Deferred: scroll into view.
        public void ScrollIntoView(bool alignToTop) { }

        // Deferred: embedded objects.
        public UIA.IRawElementProviderSimple[] GetChildren() => Array.Empty<UIA.IRawElementProviderSimple>();

        private static AAP.ITextRangeProvider Unwrap(UIA.ITextRangeProvider range)
            => range is AutomationTextRangeProvider wrapper
                ? wrapper._range
                : throw new ArgumentException("The range was not produced by this provider.", nameof(range));

        private static AAP.TextRangeEndpoint Map(UIA.TextPatternRangeEndpoint endpoint)
            => endpoint == UIA.TextPatternRangeEndpoint.End ? AAP.TextRangeEndpoint.End : AAP.TextRangeEndpoint.Start;

        private static AvTextUnit Map(UIA.TextUnit unit) => unit switch
        {
            UIA.TextUnit.Character => AvTextUnit.Character,
            UIA.TextUnit.Format => AvTextUnit.Format,
            UIA.TextUnit.Word => AvTextUnit.Word,
            UIA.TextUnit.Line => AvTextUnit.Line,
            UIA.TextUnit.Paragraph => AvTextUnit.Paragraph,
            UIA.TextUnit.Page => AvTextUnit.Page,
            UIA.TextUnit.Document => AvTextUnit.Document,
            _ => AvTextUnit.Character,
        };
    }
}
