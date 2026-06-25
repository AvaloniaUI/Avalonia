using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Utils;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI Text interface for read-only text content.
    /// </summary>
    internal sealed class AtSpiTextHandler(AtSpiNode node) : IOrgA11yAtspiText
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

        public uint Version => TextVersion;

        public int CharacterCount => Navigation is { } nav ? nav.DocumentEnd.Offset : GetText().Length;

        public int CaretOffset => Navigation is { } nav ? nav.GetSelection().End.Offset : 0;

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetStringAtOffsetAsync(
            int offset, uint granularity)
        {
            if (Navigation is { } nav)
            {
                var position = nav.GetPosition(nav.DocumentStart, offset);
                var range = nav.GetRangeEnclosing(position, MapGranularity((TextGranularity)granularity));
                return ValueTask.FromResult((nav.GetText(range), range.Start.Offset, range.End.Offset));
            }

            var text = GetText();
            if (text.Length == 0)
                return ValueTask.FromResult((string.Empty, 0, 0));

            offset = Math.Max(0, Math.Min(offset, text.Length - 1));

            var g = (TextGranularity)granularity;

            // For CHAR granularity, return single character
            if (g == TextGranularity.Char)
            {
                var ch = text.Substring(offset, 1);
                return ValueTask.FromResult((ch, offset, offset + 1));
            }

            // For WORD granularity, find word boundaries
            if (g == TextGranularity.Word)
            {
                var start = StringUtils.PreviousWord(text, offset + 1);
                if (start >= text.Length || !StringUtils.IsStartOfWord(text, start))
                    return ValueTask.FromResult((string.Empty, 0, 0));

                var end = Math.Min(StringUtils.NextWord(text, start), text.Length);
                if (end <= start)
                    return ValueTask.FromResult((string.Empty, 0, 0));

                return ValueTask.FromResult((text.Substring(start, end - start), start, end));
            }

            // For SENTENCE, LINE, PARAGRAPH - return full text
            return ValueTask.FromResult((text, 0, text.Length));
        }

        public ValueTask<string> GetTextAsync(int startOffset, int endOffset)
        {
            var text = GetText();
            if (text.Length == 0)
                return ValueTask.FromResult(string.Empty);

            startOffset = Math.Max(0, startOffset);
            if (endOffset < 0 || endOffset > text.Length)
                endOffset = text.Length;

            if (startOffset >= endOffset)
                return ValueTask.FromResult(string.Empty);

            return ValueTask.FromResult(text.Substring(startOffset, endOffset - startOffset));
        }

        public ValueTask<bool> SetCaretOffsetAsync(int offset)
        {
            if (Navigation is { } nav)
            {
                var position = nav.GetPosition(nav.DocumentStart, offset);
                nav.SetSelection(nav.GetRange(position, position));
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextBeforeOffsetAsync(
            int offset, uint type)
        {
            // TODO: This method is a bit sketchy. Might need to wired up to
            // our text handling logic in core.
            
            var text = GetText();
            if (offset <= 0 || text.Length == 0)
                return ValueTask.FromResult((string.Empty, 0, 0));

            offset = Math.Min(offset, text.Length);

            var bt = (TextBoundaryType)type;

            // CHAR boundary
            if (bt == TextBoundaryType.Char)
            {
                var charOffset = offset - 1;
                return ValueTask.FromResult((text.Substring(charOffset, 1), charOffset, charOffset + 1));
            }

            // WORD_START or WORD_END boundary
            if (bt is TextBoundaryType.WordStart or TextBoundaryType.WordEnd)
            {
                var end = offset;
                var start = StringUtils.PreviousWord(text, end);
                if (start >= end)
                    start = StringUtils.PreviousWord(text, start);

                if (start < 0 || start >= text.Length || !StringUtils.IsStartOfWord(text, start))
                    return ValueTask.FromResult((string.Empty, 0, 0));

                end = Math.Min(StringUtils.NextWord(text, start), end);
                if (end <= start)
                    return ValueTask.FromResult((string.Empty, 0, 0));

                return ValueTask.FromResult((text.Substring(start, end - start), start, end));
            }

            // SENTENCE/LINE/PARAGRAPH - return all text before offset
            var result = text.Substring(0, offset);
            return ValueTask.FromResult((result, 0, offset));
        }

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextAtOffsetAsync(
            int offset, uint type)
        {
            return GetStringAtOffsetAsync(offset, type);
        }

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetTextAfterOffsetAsync(
            int offset, uint type)
        {
            var text = GetText();
            if (offset >= text.Length - 1 || text.Length == 0)
                return ValueTask.FromResult((string.Empty, text.Length, text.Length));

            var bt = (TextBoundaryType)type;

            // CHAR boundary
            if (bt == TextBoundaryType.Char)
            {
                var charOffset = offset + 1;
                if (charOffset >= text.Length)
                    return ValueTask.FromResult((string.Empty, text.Length, text.Length));
                return ValueTask.FromResult((text.Substring(charOffset, 1), charOffset, charOffset + 1));
            }

            // WORD_START or WORD_END boundary
            if (bt is TextBoundaryType.WordStart or TextBoundaryType.WordEnd)
            {
                var start = offset + 1;

                while (start < text.Length &&
                       StringUtils.IsEndOfWord(text, start) &&
                       !StringUtils.IsStartOfWord(text, start))
                {
                    start++;
                }

                if (start >= text.Length)
                    return ValueTask.FromResult((string.Empty, text.Length, text.Length));

                var end = Math.Min(StringUtils.NextWord(text, start), text.Length);
                if (end <= start)
                    return ValueTask.FromResult((string.Empty, text.Length, text.Length));

                return ValueTask.FromResult((text.Substring(start, end - start), start, end));
            }

            // SENTENCE/LINE/PARAGRAPH - return all text after offset
            var afterOffset = Math.Max(0, offset + 1);
            var result = text.Substring(afterOffset);
            return ValueTask.FromResult((result, afterOffset, text.Length));
        }

        public ValueTask<int> GetCharacterAtOffsetAsync(int offset)
        {
            var text = GetText();
            if (offset < 0 || offset >= text.Length)
                return ValueTask.FromResult(unchecked((int)0xFFFFFFFF));

            return ValueTask.FromResult((int)text[offset]);
        }

        public ValueTask<string> GetAttributeValueAsync(int offset, string attributeName)
        {
            if (Navigation is { } nav)
            {
                var attributes = ToAtSpi(nav.GetTextAttributes(nav.GetPosition(nav.DocumentStart, offset)).Attributes);
                if (attributes.TryGetValue(attributeName, out var value))
                    return ValueTask.FromResult(value);
            }

            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributesAsync(int offset)
        {
            if (Navigation is { } nav)
            {
                var (attributes, run) = nav.GetTextAttributes(nav.GetPosition(nav.DocumentStart, offset));
                return ValueTask.FromResult((ToAtSpi(attributes), run.Start.Offset, run.End.Offset));
            }

            return ValueTask.FromResult((new AtSpiAttributeSet(), 0, GetText().Length));
        }

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributesAsync()
            => ValueTask.FromResult(Navigation is { } nav
                ? ToAtSpi(nav.GetTextAttributes(nav.DocumentStart).Attributes)
                : new AtSpiAttributeSet());

        public ValueTask<(int X, int Y, int Width, int Height)> GetCharacterExtentsAsync(
            int offset, uint coordType)
        {
            if (Navigation is { } nav)
            {
                var start = nav.GetPosition(nav.DocumentStart, offset);
                var end = nav.GetPosition(start, TextUnit.Character, 1);
                if (TryGetExtents(nav, nav.GetRange(start, end), coordType, out var extents))
                    return ValueTask.FromResult(extents);
            }

            return ValueTask.FromResult((0, 0, 0, 0));
        }

        public ValueTask<int> GetOffsetAtPointAsync(int x, int y, uint coordType)
        {
            return ValueTask.FromResult(-1);
        }

        public ValueTask<int> GetNSelectionsAsync()
            => ValueTask.FromResult(Navigation is { } nav && !nav.GetSelection().IsEmpty ? 1 : 0);

        public ValueTask<(int StartOffset, int EndOffset)> GetSelectionAsync(int selectionNum)
        {
            if (Navigation is { } nav)
            {
                var selection = nav.GetSelection();
                return ValueTask.FromResult((selection.Start.Offset, selection.End.Offset));
            }

            return ValueTask.FromResult((0, 0));
        }

        public ValueTask<bool> AddSelectionAsync(int startOffset, int endOffset)
            => SetSelectionAsync(0, startOffset, endOffset);

        // A single-selection control cannot remove its only selection.
        public ValueTask<bool> RemoveSelectionAsync(int selectionNum)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> SetSelectionAsync(int selectionNum, int startOffset, int endOffset)
        {
            if (Navigation is { } nav)
            {
                nav.SetSelection(nav.GetRange(
                    nav.GetPosition(nav.DocumentStart, startOffset),
                    nav.GetPosition(nav.DocumentStart, endOffset)));
                return ValueTask.FromResult(true);
            }

            return ValueTask.FromResult(false);
        }

        public ValueTask<(int X, int Y, int Width, int Height)> GetRangeExtentsAsync(
            int startOffset, int endOffset, uint coordType)
        {
            if (Navigation is { } nav)
            {
                var range = nav.GetRange(
                    nav.GetPosition(nav.DocumentStart, startOffset),
                    nav.GetPosition(nav.DocumentStart, endOffset));
                if (TryGetExtents(nav, range, coordType, out var extents))
                    return ValueTask.FromResult(extents);
            }

            return ValueTask.FromResult((0, 0, 0, 0));
        }

        public ValueTask<List<AtSpiTextRange>> GetBoundedRangesAsync(
            int x, int y, int width, int height, uint coordType, uint xClipType, uint yClipType)
        {
            return ValueTask.FromResult(new List<AtSpiTextRange>());
        }

        // A TextBox is uniform, so the attribute run is the same set the whole document carries.
        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributeRunAsync(
            int offset, bool includeDefaults)
            => GetAttributesAsync(offset);

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributeSetAsync()
            => GetDefaultAttributesAsync();

        public ValueTask<bool> ScrollSubstringToAsync(int startOffset, int endOffset, uint type)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> ScrollSubstringToPointAsync(
            int startOffset, int endOffset, uint coordType, int x, int y)
        {
            return ValueTask.FromResult(false);
        }

        private IAccessibleText? Navigation => node.Peer.GetProvider<IAccessibleText>();

        private static TextUnit MapGranularity(TextGranularity granularity) => granularity switch
        {
            TextGranularity.Word => TextUnit.Word,
            TextGranularity.Sentence => TextUnit.Sentence,
            TextGranularity.Line => TextUnit.Line,
            TextGranularity.Paragraph => TextUnit.Paragraph,
            _ => TextUnit.Character,
        };

        // Maps the shared attribute vocabulary onto AT-SPI's Pango/ATK attribute names. Read-only is
        // intentionally omitted: AT-SPI exposes editability through the state set, not a text attribute.
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
                        set["size"] = size.ToString("0.##", CultureInfo.InvariantCulture);
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

        // Unions the per-line top-level rects of a range and maps them to the requested coordinate space.
        private bool TryGetExtents(
            IAccessibleText nav, ITextRange range, uint coordType, out (int X, int Y, int Width, int Height) extents)
        {
            var rects = nav.GetBoundingRectangles(range);
            if (rects.Length == 0)
            {
                extents = (0, 0, 0, 0);
                return false;
            }

            var union = rects[0];
            for (var i = 1; i < rects.Length; i++)
                union = union.Union(rects[i]);

            var screen = AtSpiCoordinateHelper.ToScreenRect(node, union);
            var translated = AtSpiCoordinateHelper.TranslateRect(node, screen, coordType);
            extents = ((int)translated.X, (int)translated.Y, (int)translated.Width, (int)translated.Height);
            return true;
        }

        private string GetText()
        {
            if (Navigation is { } nav)
            {
                return nav.GetText(nav.DocumentRange);
            }

            return node.Peer.GetProvider<IValueProvider>()?.Value ?? string.Empty;
        }
    }
}
