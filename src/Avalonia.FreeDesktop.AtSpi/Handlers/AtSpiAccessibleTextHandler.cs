using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.DBus;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Text interface over a control's <see cref="IAccessibleText"/>.
    /// </summary>
    /// <remarks>
    /// Offsets are characters (code points) in the <see cref="AtSpiTextModel"/> view; unit boundaries
    /// come from the control's navigation. Text is only ever read as drawn, so a masked password is
    /// reported as its mask.
    /// </remarks>
    internal sealed class AtSpiAccessibleTextHandler(AtSpiNode node, AtSpiTextModel model) : IOrgA11yAtspiText
    {
        private enum TextGranularity : uint
        {
            Char = 0,
            Word = 1,
            Sentence = 2,
            Line = 3,
            Paragraph = 4,
        }

        private enum TextBoundaryType : uint
        {
            Char = 0,
            WordStart = 1,
            WordEnd = 2,
            SentenceStart = 3,
            SentenceEnd = 4,
            LineStart = 5,
            LineEnd = 6,
        }

        // AT-SPI reports font sizes in points; Avalonia sizes are device-independent pixels (1/96 inch).
        private const double PointsPerDip = 72.0 / 96.0;

        private IAccessibleText Text => model.Text;

        public uint Version => TextVersion;

        public int CharacterCount => model.Length;

        public int CaretOffset => model.ToOffset(Text.GetCaret().Start);

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetStringAtOffsetAsync(
            int offset, uint granularity)
        {
            // Every granularity runs from the start of its unit to the start of the next.
            var range = (TextGranularity)granularity switch
            {
                TextGranularity.Char => CharRange(offset),
                TextGranularity.Word => StartRange(offset, TextUnit.Word),
                TextGranularity.Sentence => StartRange(offset, TextUnit.Sentence),
                TextGranularity.Line => StartRange(offset, TextUnit.Line),
                TextGranularity.Paragraph => StartRange(offset, TextUnit.Paragraph),
                _ => ((int, int)?)null,
            };

            return Result(range);
        }

        public ValueTask<string> GetTextAsync(int startOffset, int endOffset)
        {
            if (endOffset < 0)
            {
                endOffset = model.Length;
            }

            return ValueTask.FromResult(model.Substring(startOffset, endOffset));
        }

        public ValueTask<bool> SetCaretOffsetAsync(int offset)
        {
            if (offset < 0 || offset > model.Length)
            {
                return ValueTask.FromResult(false);
            }

            var position = model.ToPosition(offset);
            Text.SetSelection(new DirectedTextRange(position, position));
            return ValueTask.FromResult(true);
        }

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextBeforeOffsetAsync(int offset, uint type)
        {
            var at = BoundaryRange(offset, (TextBoundaryType)type);
            return Result(at is { } current && current.Start > 0 ? BoundaryRange(current.Start - 1, (TextBoundaryType)type) : null);
        }

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextAtOffsetAsync(int offset, uint type)
            => Result(BoundaryRange(offset, (TextBoundaryType)type));

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextAfterOffsetAsync(int offset, uint type)
        {
            var at = BoundaryRange(offset, (TextBoundaryType)type);
            return Result(at is { } current && current.End < model.Length
                ? BoundaryRange(current.End, (TextBoundaryType)type)
                : null);
        }

        public ValueTask<int> GetCharacterAtOffsetAsync(int offset) => ValueTask.FromResult(model.CharacterAt(offset));

        public ValueTask<string> GetAttributeValueAsync(int offset, string attributeName)
        {
            var attributes = ToAtSpi(Text.GetTextAttributes(model.ToPosition(offset)).Attributes);
            return ValueTask.FromResult(attributes.TryGetValue(attributeName, out var value) ? value : string.Empty);
        }

        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributesAsync(int offset)
            => GetAttributeRunAsync(offset, includeDefaults: false);

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributesAsync() => ValueTask.FromResult(DefaultAttributes());

        public ValueTask<(int X, int Y, int Width, int Height)> GetCharacterExtentsAsync(int offset, uint coordType)
        {
            if (offset < 0 || offset >= model.Length)
            {
                return ValueTask.FromResult((0, 0, 0, 0));
            }

            return ValueTask.FromResult(Extents(model.ToRange(offset, offset + 1), coordType));
        }

        public ValueTask<int> GetOffsetAtPointAsync(int x, int y, uint coordType)
        {
            var point = AtSpiCoordinateHelper.PointToTopLevel(node, x, y, coordType);
            return ValueTask.FromResult(OffsetAtPoint(point) ?? -1);
        }

        public ValueTask<int> GetNSelectionsAsync() => ValueTask.FromResult(Text.GetSelection().IsEmpty ? 0 : 1);

        public ValueTask<(int StartOffset, int EndOffset)> GetSelectionAsync(int selectionNum)
        {
            var selection = Text.GetSelection();
            if (selectionNum != 0 || selection.IsEmpty)
            {
                return ValueTask.FromResult((0, 0));
            }

            return ValueTask.FromResult((model.ToOffset(selection.Start), model.ToOffset(selection.End)));
        }

        public ValueTask<bool> AddSelectionAsync(int startOffset, int endOffset)
        {
            // A single-selection control can add a selection only while it has none.
            if (!Text.GetSelection().IsEmpty)
            {
                return ValueTask.FromResult(false);
            }

            return ValueTask.FromResult(Select(startOffset, endOffset));
        }

        public ValueTask<bool> RemoveSelectionAsync(int selectionNum)
        {
            var selection = Text.GetSelection();
            if (selectionNum != 0 || selection.IsEmpty)
            {
                return ValueTask.FromResult(false);
            }

            // Removing the only selection collapses it to the caret.
            var caret = selection.Active;
            Text.SetSelection(new DirectedTextRange(caret, caret));
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> SetSelectionAsync(int selectionNum, int startOffset, int endOffset)
            => ValueTask.FromResult(selectionNum == 0 && Select(startOffset, endOffset));

        public ValueTask<(int X, int Y, int Width, int Height)> GetRangeExtentsAsync(
            int startOffset, int endOffset, uint coordType)
            => ValueTask.FromResult(Extents(model.ToRange(startOffset, endOffset), coordType));

        public ValueTask<List<AtSpiTextRange>> GetBoundedRangesAsync(
            int x, int y, int width, int height, uint coordType, uint xClipType, uint yClipType)
        {
            var result = new List<AtSpiTextRange>();
            var topLeft = AtSpiCoordinateHelper.PointToTopLevel(node, x, y, coordType);
            var bottomRight = AtSpiCoordinateHelper.PointToTopLevel(node, x + width, y + height, coordType);
            var box = new Rect(topLeft, bottomRight);

            if (Text.GetPositionFromPoint(topLeft) is not { } first ||
                Text.GetPositionFromPoint(new Point(topLeft.X, bottomRight.Y)) is not { } last)
            {
                return ValueTask.FromResult(result);
            }

            // One range per line: the part of each line between the box's left and right edges.
            var line = Text.GetRangeEnclosing(first, TextUnit.Line);
            var lastLine = Text.GetRangeEnclosing(last, TextUnit.Line);
            while (true)
            {
                var rects = Text.GetBoundingRectangles(line);
                if (rects.Length > 0)
                {
                    var lineRect = Union(rects);
                    if (lineRect.Intersects(box))
                    {
                        var centerY = lineRect.Center.Y;
                        var from = Text.GetPositionFromPoint(new Point(box.Left, centerY));
                        var to = Text.GetPositionFromPoint(new Point(box.Right, centerY));
                        if (from is not null && to is not null)
                        {
                            var start = model.ToOffset(from);
                            var end = model.ToOffset(to);
                            if (end > start)
                            {
                                result.Add(new AtSpiTextRange(start, end, model.Substring(start, end), new DBusVariant(string.Empty)));
                            }
                        }
                    }
                }

                if (line.Start.CompareTo(lastLine.Start) >= 0)
                {
                    break;
                }

                var next = Text.GetPosition(Text.WithGravity(line.Start, LogicalDirection.Forward), TextUnit.Line, 1);
                if (next.CompareTo(line.Start) <= 0)
                {
                    break;
                }

                line = Text.GetRangeEnclosing(next, TextUnit.Line);
            }

            return ValueTask.FromResult(result);
        }

        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributeRunAsync(
            int offset, bool includeDefaults)
        {
            var (attributes, run) = Text.GetTextAttributes(model.ToPosition(offset));
            var set = ToAtSpi(attributes);

            if (!includeDefaults)
            {
                // Without defaults the run reports only what differs from the document's defaults.
                foreach (var (name, value) in DefaultAttributes())
                {
                    if (set.TryGetValue(name, out var current) && current == value)
                    {
                        set.Remove(name);
                    }
                }
            }

            return ValueTask.FromResult((set, model.ToOffset(run.Start), model.ToOffset(run.End)));
        }

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributeSetAsync() => ValueTask.FromResult(DefaultAttributes());

        public ValueTask<bool> ScrollSubstringToAsync(int startOffset, int endOffset, uint type)
        {
            if (!IsValidSpan(startOffset, endOffset))
            {
                return ValueTask.FromResult(false);
            }

            Text.ScrollIntoView(model.ToRange(startOffset, endOffset));
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> ScrollSubstringToPointAsync(
            int startOffset, int endOffset, uint coordType, int x, int y)
        {
            return ValueTask.FromResult(false);
        }

        private bool Select(int startOffset, int endOffset)
        {
            if (!IsValidSpan(Math.Min(startOffset, endOffset), Math.Max(startOffset, endOffset)))
            {
                return false;
            }

            // AT-SPI selects from start to end, so the caret lands on the end offset either way round.
            Text.SetSelection(new DirectedTextRange(model.ToPosition(startOffset), model.ToPosition(endOffset)));
            return true;
        }

        private bool IsValidSpan(int start, int end) => start >= 0 && end >= start && end <= model.Length;

        private (int Start, int End)? CharRange(int offset)
            => offset < 0 || offset >= model.Length ? null : (offset, offset + 1);

        private (int Start, int End)? BoundaryRange(int offset, TextBoundaryType type) => type switch
        {
            TextBoundaryType.Char => CharRange(offset),
            TextBoundaryType.WordStart => StartRange(offset, TextUnit.Word),
            TextBoundaryType.WordEnd => EndRange(offset, TextUnit.Word),
            TextBoundaryType.SentenceStart => StartRange(offset, TextUnit.Sentence),
            TextBoundaryType.SentenceEnd => EndRange(offset, TextUnit.Sentence),
            TextBoundaryType.LineStart => StartRange(offset, TextUnit.Line),
            TextBoundaryType.LineEnd => EndRange(offset, TextUnit.Line),
            _ => null,
        };

        // From the start of the unit containing the offset to the start of the next unit, so a word
        // carries its trailing spaces and a line its terminator.
        private (int Start, int End)? StartRange(int offset, TextUnit unit)
        {
            if (offset < 0 || offset >= model.Length)
            {
                return null;
            }

            var enclosing = Text.GetRangeEnclosing(model.ToPosition(offset), unit);
            var start = model.ToOffset(enclosing.Start);
            var next = NextUnitStart(enclosing.Start, unit);
            var end = next is null ? model.Length : model.ToOffset(next);

            return (start, Math.Max(end, start + 1));
        }

        // The start of the unit after the one starting at `start`, or null at the document end. A
        // line's content ends before its terminator, and a step by line stops there, so the walk
        // continues by character until it reaches a position where a unit begins.
        private ITextPointer? NextUnitStart(ITextPointer start, TextUnit unit)
        {
            var next = Text.GetPosition(Text.WithGravity(start, LogicalDirection.Forward), unit, 1);
            if (next.CompareTo(start) <= 0)
            {
                return null;
            }

            while (!Text.GetRangeEnclosing(Text.WithGravity(next, LogicalDirection.Forward), unit).Start.Equals(next))
            {
                var after = Text.GetPosition(next, TextUnit.Character, 1);
                if (after.CompareTo(next) <= 0)
                {
                    return null;
                }

                next = after;
            }

            return next;
        }

        // From the end of the previous unit's content to the end of this unit's content, so a word
        // carries the spaces before it. An offset in the spaces after a word belongs to the next word.
        private (int Start, int End)? EndRange(int offset, TextUnit unit)
        {
            if (offset < 0 || offset >= model.Length)
            {
                return null;
            }

            var enclosing = Text.GetRangeEnclosing(model.ToPosition(offset), unit);
            var end = ContentEnd(enclosing);

            if (offset >= end)
            {
                var next = NextUnitStart(enclosing.Start, unit);
                if (next is null || model.ToOffset(next) >= model.Length)
                {
                    return (end, model.Length);
                }

                return (end, Math.Max(ContentEnd(Text.GetRangeEnclosing(next, unit)), end + 1));
            }

            return (PreviousContentEnd(enclosing, unit), end);
        }

        private int PreviousContentEnd(ITextRange enclosing, TextUnit unit)
        {
            if (model.ToOffset(enclosing.Start) == 0)
            {
                return 0;
            }

            var before = Text.GetRangeEnclosing(Text.WithGravity(enclosing.Start, LogicalDirection.Backward), unit);
            return before.Start.Equals(enclosing.Start) ? 0 : ContentEnd(before);
        }

        // The offset just past the unit's last non-whitespace character.
        private int ContentEnd(ITextRange range)
        {
            var text = Text.GetPlainText(range).TrimEnd();
            return model.ToOffset(range.Start) + AtSpiTextModel.CodePoints(text);
        }

        private ValueTask<(string Text, int StartOffset, int EndOffset)> Result((int Start, int End)? range)
            => ValueTask.FromResult(range is { } r
                ? (model.Substring(r.Start, r.End), r.Start, r.End)
                : (string.Empty, 0, 0));

        // The character under the point, or null when the point is not over any text.
        private int? OffsetAtPoint(Point point)
        {
            if (Text.GetPositionFromPoint(point) is not { } hit)
            {
                return null;
            }

            var offset = model.ToOffset(hit);

            // The hit test answers the nearest position; accept it only when the point lies on the
            // character after it or the one before it.
            foreach (var candidate in new[] { offset, offset - 1 })
            {
                if (candidate < 0 || candidate >= model.Length)
                {
                    continue;
                }

                foreach (var rect in Text.GetBoundingRectangles(model.ToRange(candidate, candidate + 1)))
                {
                    if (rect.Contains(point))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private (int X, int Y, int Width, int Height) Extents(ITextRange range, uint coordType)
        {
            var rects = Text.GetBoundingRectangles(range);
            if (rects.Length == 0)
            {
                return (0, 0, 0, 0);
            }

            var screen = AtSpiCoordinateHelper.ToScreenRect(node, Union(rects));
            var translated = AtSpiCoordinateHelper.TranslateRect(node, screen, coordType);
            return ((int)translated.X, (int)translated.Y, (int)translated.Width, (int)translated.Height);
        }

        private static Rect Union(Rect[] rects)
        {
            var union = rects[0];
            for (var i = 1; i < rects.Length; i++)
            {
                union = union.Union(rects[i]);
            }

            return union;
        }

        private AtSpiAttributeSet DefaultAttributes() => ToAtSpi(Text.GetTextAttributes(Text.DocumentStart).Attributes);

        // Maps the shared attribute vocabulary onto AT-SPI's Pango/ATK attribute names. Read-only is
        // left out: AT-SPI reports editability through the state set, not a text attribute.
        private static AtSpiAttributeSet ToAtSpi(IReadOnlyDictionary<TextAttribute, object?> attributes)
        {
            var set = new AtSpiAttributeSet();

            foreach (var (attribute, value) in attributes)
            {
                switch (attribute)
                {
                    case TextAttribute.FontFamily when value is string family:
                        set["family-name"] = family;
                        break;
                    case TextAttribute.FontSize when value is double size:
                        set["size"] = (size * PointsPerDip).ToString("0.##", CultureInfo.InvariantCulture);
                        break;
                    case TextAttribute.FontWeight when value is FontWeight weight:
                        set["weight"] = ((int)weight).ToString(CultureInfo.InvariantCulture);
                        break;
                    case TextAttribute.FontStyle when value is FontStyle style:
                        set["style"] = style switch
                        {
                            FontStyle.Italic => "italic",
                            FontStyle.Oblique => "oblique",
                            _ => "normal",
                        };
                        break;
                    case TextAttribute.Foreground when value is Color fg:
                        set["fg-color"] = $"{fg.R},{fg.G},{fg.B}";
                        break;
                    case TextAttribute.Background when value is Color bg:
                        set["bg-color"] = $"{bg.R},{bg.G},{bg.B}";
                        break;
                }
            }

            return set;
        }
    }
}
