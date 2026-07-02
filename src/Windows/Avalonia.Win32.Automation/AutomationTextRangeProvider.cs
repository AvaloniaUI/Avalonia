using System;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using Avalonia.Media;
using AAP = Avalonia.Automation.Provider;
using AvTextAttribute = Avalonia.Input.TextInput.TextAttribute;
using AvTextStyleId = Avalonia.Input.TextInput.TextStyleId;
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

        // UIA invokes these on its own thread; every call into the cross-platform range (which touches
        // Avalonia UI objects) is marshalled onto the UI thread via the owner's InvokeSync.

        public UIA.ITextRangeProvider Clone()
            => _owner.InvokeSync<UIA.ITextRangeProvider>(() => new AutomationTextRangeProvider(_owner, _range.Clone()));

        public bool Compare(UIA.ITextRangeProvider range) => _owner.InvokeSync(() => _range.Compare(Unwrap(range)));

        public int CompareEndpoints(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange,
            UIA.TextPatternRangeEndpoint targetEndpoint)
            => _owner.InvokeSync(() => _range.CompareEndpoints(Map(endpoint), Unwrap(targetRange), Map(targetEndpoint)));

        public void ExpandToEnclosingUnit(UIA.TextUnit unit)
            => _owner.InvokeSync(() => _range.ExpandToEnclosingUnit(Map(unit)));

        public UIA.ITextRangeProvider FindAttribute(int attribute, object value, bool backward)
            => _owner.InvokeSync<UIA.ITextRangeProvider>(() =>
            {
                // Attributes are uniform over a plain TextBox: the whole range matches iff its value
                // equals the query (a richer control would search for the sub-range carrying the value).
                var current = GetAttributeValueCore(attribute);
                return current is not null && current.Equals(value)
                    ? new AutomationTextRangeProvider(_owner, _range.Clone())
                    : null!;
            });

        public UIA.ITextRangeProvider FindText(string text, bool backward, bool ignoreCase)
            => _owner.InvokeSync<UIA.ITextRangeProvider>(() =>
            {
                var match = _range.FindText(text, backward, ignoreCase);
                return match is null ? null! : new AutomationTextRangeProvider(_owner, match);
            });

        public object GetAttributeValue(int attribute) => _owner.InvokeSync(() => GetAttributeValueCore(attribute));

        private object GetAttributeValueCore(int attribute)
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
                // UIA StyleId values are StyleId_Custom (70000) + ordinal, so Heading1 -> 70001, etc.
                UiaTextAttributeStyleId =>
                    _range.GetAttributeValue(AvTextAttribute.StyleId) is AvTextStyleId styleId ? 70000 + (int)styleId : (int?)null,
                _ => null,
            };

            return value!;
        }

        public double[] GetBoundingRectangles()
            => _owner.InvokeSync(() => _owner.PointsToScreen(_range.GetBoundingRectangles()));

        public UIA.IRawElementProviderSimple GetEnclosingElement()
            => _owner.InvokeSync<UIA.IRawElementProviderSimple>(() =>
                (_range.GetEnclosingElement() is { } enclosing ? AutomationNode.GetOrCreate(enclosing) : null) ?? _owner);

        public string GetText(int maxLength) => _owner.InvokeSync(() => _range.GetText(maxLength));

        public int Move(UIA.TextUnit unit, int count) => _owner.InvokeSync(() => _range.Move(Map(unit), count));

        public int MoveEndpointByUnit(UIA.TextPatternRangeEndpoint endpoint, UIA.TextUnit unit, int count)
            => _owner.InvokeSync(() => _range.MoveEndpointByUnit(Map(endpoint), Map(unit), count));

        public void MoveEndpointByRange(UIA.TextPatternRangeEndpoint endpoint, UIA.ITextRangeProvider targetRange,
            UIA.TextPatternRangeEndpoint targetEndpoint)
            => _owner.InvokeSync(() => _range.MoveEndpointByRange(Map(endpoint), Unwrap(targetRange), Map(targetEndpoint)));

        public void Select() => _owner.InvokeSync(() => _range.Select());

        // A single-selection control treats adding-to-selection as replacing it; removal is a no-op.
        public void AddToSelection() => _owner.InvokeSync(() => _range.Select());
        public void RemoveFromSelection() { }

        public void ScrollIntoView(bool alignToTop) => _owner.InvokeSync(() => _range.ScrollIntoView(alignToTop));

        public UIA.IRawElementProviderSimple[] GetChildren()
            => _owner.InvokeSync(() => _range.GetChildren()
                .Select(peer => (UIA.IRawElementProviderSimple)AutomationNode.GetOrCreate(peer)!)
                .ToArray());

        // Well-known UIA_*AttributeId values (UIAutomationTextPattern.h; text attribute ids are the
        // 40000 range - 30000 is the property-id family, which clients never use here).
        private const int UiaTextAttributeBackgroundColor = 40001;
        private const int UiaTextAttributeFontName = 40005;
        private const int UiaTextAttributeFontSize = 40006;
        private const int UiaTextAttributeFontWeight = 40007;
        private const int UiaTextAttributeForegroundColor = 40008;
        private const int UiaTextAttributeIsItalic = 40014;
        private const int UiaTextAttributeIsReadOnly = 40015;
        private const int UiaTextAttributeStyleId = 40034;

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
