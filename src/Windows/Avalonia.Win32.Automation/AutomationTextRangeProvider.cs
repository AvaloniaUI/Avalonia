using System;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Media;
using AAP = Avalonia.Automation.Provider;
using AvTextAttribute = Avalonia.Input.TextInput.TextAttribute;
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

        public UIA.ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
        {
            // Attributes are uniform over a plain TextBox: the whole range matches iff its value equals
            // the query (a richer control would search for the sub-range carrying the value).
            var current = GetAttributeValue(attribute);
            return current is not null && current.Equals(value) ? Clone() : null!;
        }

        public UIA.ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
        {
            var match = _range.FindText(text, backward, ignoreCase);
            return match is null ? null! : new AutomationTextRangeProvider(_owner, match);
        }

        public object GetAttributeValue(int attribute)
        {
            // Map the well-known UIA TextAttributeId onto the shared vocabulary, then to its UIA form;
            // an unsupported attribute (or one that varies across the range) returns an empty value.
            object? value = attribute switch
            {
                UiaTextAttributeFontName => _range.GetAttributeValue(AvTextAttribute.FontFamily),
                UiaTextAttributeFontSize => _range.GetAttributeValue(AvTextAttribute.FontSize),
                UiaTextAttributeFontWeight =>
                    _range.GetAttributeValue(AvTextAttribute.FontWeight) is FontWeight weight ? (int)weight : null,
                UiaTextAttributeIsItalic =>
                    _range.GetAttributeValue(AvTextAttribute.FontStyle) is FontStyle style ? style != FontStyle.Normal : null,
                UiaTextAttributeForegroundColor =>
                    _range.GetAttributeValue(AvTextAttribute.Foreground) is Color foreground ? ToColorRef(foreground) : null,
                UiaTextAttributeBackgroundColor =>
                    _range.GetAttributeValue(AvTextAttribute.Background) is Color background ? ToColorRef(background) : null,
                UiaTextAttributeIsReadOnly => _range.GetAttributeValue(AvTextAttribute.IsReadOnly),
                _ => null,
            };

            return value!;
        }

        public double[] GetBoundingRectangles() => _owner.PointsToScreen(_range.GetBoundingRectangles());

        public UIA.IRawElementProviderSimple GetEnclosingElement() => _owner;

        public string GetText(int maxLength) => _range.GetText(maxLength);

        public int Move(UIA.TextUnit unit, int count) => _range.Move(Map(unit), count);

        public int MoveEndpointByUnit(UIA.TextPatternRangeEndpoint endpoint, UIA.TextUnit unit, int count)
            => _range.MoveEndpointByUnit(Map(endpoint), Map(unit), count);

        public void MoveEndpointByRange(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange,
            UIA.TextPatternRangeEndpoint targetEndpoint)
            => _range.MoveEndpointByRange(Map(endpoint), Unwrap(targetRange), Map(targetEndpoint));

        public void Select() => _range.Select();

        // A single-selection control treats adding-to-selection as replacing it; removal is a no-op.
        public void AddToSelection() => _range.Select();
        public void RemoveFromSelection() { }

        public void ScrollIntoView(bool alignToTop) => _range.ScrollIntoView(alignToTop);

        // A plain TextBox has no embedded objects.
        public UIA.IRawElementProviderSimple[] GetChildren() => Array.Empty<UIA.IRawElementProviderSimple>();

        // Well-known UIA TextAttributeId values (UIAutomationClient.h).
        private const int UiaTextAttributeFontName = 30001;
        private const int UiaTextAttributeFontSize = 30002;
        private const int UiaTextAttributeFontWeight = 30003;
        private const int UiaTextAttributeForegroundColor = 30008;
        private const int UiaTextAttributeBackgroundColor = 30009;
        private const int UiaTextAttributeIsItalic = 30016;
        private const int UiaTextAttributeIsReadOnly = 30024;

        // UIA colour attributes are a COLORREF (0x00BBGGRR).
        private static int ToColorRef(Color color) => color.R | (color.G << 8) | (color.B << 16);

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
