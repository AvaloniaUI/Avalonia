using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Utils;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
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

        public int CharacterCount => GetText().Length;

        public int CaretOffset => 0;

        public ValueTask<(string Text, int StartOffset, int EndOffset)> GetStringAtOffsetAsync(
            int offset, uint granularity)
        {
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
            return ValueTask.FromResult(string.Empty);
        }

        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributesAsync(int offset)
        {
            var text = GetText();
            return ValueTask.FromResult((new AtSpiAttributeSet(), 0, text.Length));
        }

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributesAsync()
        {
            return ValueTask.FromResult(new AtSpiAttributeSet());
        }

        public ValueTask<(int X, int Y, int Width, int Height)> GetCharacterExtentsAsync(
            int offset, uint coordType)
        {
            return ValueTask.FromResult((0, 0, 0, 0));
        }

        public ValueTask<int> GetOffsetAtPointAsync(int x, int y, uint coordType)
        {
            return ValueTask.FromResult(-1);
        }

        public ValueTask<int> GetNSelectionsAsync()
        {
            return ValueTask.FromResult(0);
        }

        public ValueTask<(int StartOffset, int EndOffset)> GetSelectionAsync(int selectionNum)
        {
            return ValueTask.FromResult((0, 0));
        }

        public ValueTask<bool> AddSelectionAsync(int startOffset, int endOffset)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> RemoveSelectionAsync(int selectionNum)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> SetSelectionAsync(int selectionNum, int startOffset, int endOffset)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<(int X, int Y, int Width, int Height)> GetRangeExtentsAsync(
            int startOffset, int endOffset, uint coordType)
        {
            return ValueTask.FromResult((0, 0, 0, 0));
        }

        public ValueTask<List<AtSpiTextRange>> GetBoundedRangesAsync(
            int x, int y, int width, int height, uint coordType, uint xClipType, uint yClipType)
        {
            return ValueTask.FromResult(new List<AtSpiTextRange>());
        }

        public ValueTask<(AtSpiAttributeSet Attributes, int StartOffset, int EndOffset)> GetAttributeRunAsync(
            int offset, bool includeDefaults)
        {
            var text = GetText();
            return ValueTask.FromResult((new AtSpiAttributeSet(), 0, text.Length));
        }

        public ValueTask<AtSpiAttributeSet> GetDefaultAttributeSetAsync()
        {
            return ValueTask.FromResult(new AtSpiAttributeSet());
        }

        public ValueTask<bool> ScrollSubstringToAsync(int startOffset, int endOffset, uint type)
        {
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> ScrollSubstringToPointAsync(
            int startOffset, int endOffset, uint coordType, int x, int y)
        {
            return ValueTask.FromResult(false);
        }

        private string GetText()
        {
            return node.Peer.GetProvider<IValueProvider>()?.Value ?? string.Empty;
        }
    }
}
