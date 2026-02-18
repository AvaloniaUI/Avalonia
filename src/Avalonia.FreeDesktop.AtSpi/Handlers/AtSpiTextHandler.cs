using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    internal sealed class AtSpiTextHandler : IOrgA11yAtspiText
    {
        private readonly AtSpiNode _node;

        public AtSpiTextHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _node = node;
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

            // For CHAR granularity, return single character
            if (granularity == 0)
            {
                var ch = text.Substring(offset, 1);
                return ValueTask.FromResult((ch, offset, offset + 1));
            }

            // For WORD granularity, find word boundaries
            if (granularity == 1)
            {
                var start = offset;
                var end = offset;
                while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
                    start--;
                while (end < text.Length && !char.IsWhiteSpace(text[end]))
                    end++;
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
            var text = GetText();
            if (offset <= 0 || text.Length == 0)
                return ValueTask.FromResult((string.Empty, 0, 0));

            offset = Math.Min(offset, text.Length);

            // CHAR boundary
            if (type == 0)
            {
                var charOffset = offset - 1;
                return ValueTask.FromResult((text.Substring(charOffset, 1), charOffset, charOffset + 1));
            }

            // WORD_START or WORD_END boundary
            if (type is 1 or 2)
            {
                var end = offset;
                // Skip whitespace before offset to find end of previous word
                while (end > 0 && char.IsWhiteSpace(text[end - 1]))
                    end--;
                if (end == 0)
                    return ValueTask.FromResult((string.Empty, 0, 0));
                var start = end;
                while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
                    start--;
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

            // CHAR boundary
            if (type == 0)
            {
                var charOffset = offset + 1;
                if (charOffset >= text.Length)
                    return ValueTask.FromResult((string.Empty, text.Length, text.Length));
                return ValueTask.FromResult((text.Substring(charOffset, 1), charOffset, charOffset + 1));
            }

            // WORD_START or WORD_END boundary
            if (type is 1 or 2)
            {
                var start = offset + 1;
                // Skip whitespace after offset to find start of next word
                while (start < text.Length && char.IsWhiteSpace(text[start]))
                    start++;
                if (start >= text.Length)
                    return ValueTask.FromResult((string.Empty, text.Length, text.Length));
                var end = start;
                while (end < text.Length && !char.IsWhiteSpace(text[end]))
                    end++;
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
            return _node.Peer.GetProvider<IValueProvider>()?.Value ?? string.Empty;
        }
    }
}
