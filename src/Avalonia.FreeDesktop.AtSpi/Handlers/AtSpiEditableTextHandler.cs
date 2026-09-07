using System;
using System.Threading.Tasks;
using Avalonia.Automation.Provider;
using Avalonia.FreeDesktop.AtSpi.DBusXml;
using static Avalonia.FreeDesktop.AtSpi.AtSpiConstants;

namespace Avalonia.FreeDesktop.AtSpi.Handlers
{
    /// <summary>
    /// Implements the AT-SPI EditableText interface for writable text content.
    /// </summary>
    /// <remarks>
    /// Edits rewrite the control's value through its <see cref="IValueProvider"/>. Offsets are AT-SPI
    /// characters; when the control navigates its text they convert through the node's text model.
    /// A control whose value is drawn masked refuses edits: the value it reports is the mask, so
    /// rebuilding it would replace the real text.
    /// </remarks>
    internal sealed class AtSpiEditableTextHandler(AtSpiNode node) : IOrgA11yAtspiEditableText
    {
        public uint Version => EditableTextVersion;

        public ValueTask<bool> SetTextContentsAsync(string newContents)
        {
            if (!TryGetEditable(out var provider, out _))
                return ValueTask.FromResult(false);

            provider.SetValue(newContents);
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> InsertTextAsync(int position, string text, int length)
        {
            if (!TryGetEditable(out var provider, out var current))
                return ValueTask.FromResult(false);

            var index = ToIndex(current, position);

            // AT-SPI passes the length in bytes of the UTF-8 encoding; clients pass the whole string, so
            // the text is inserted as given.
            provider.SetValue(current.Insert(index, text));
            return ValueTask.FromResult(true);
        }

        public ValueTask CopyTextAsync(int startPos, int endPos)
        {
            // Clipboard operations not supported via IValueProvider
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> CutTextAsync(int startPos, int endPos)
        {
            // Clipboard operations not supported via IValueProvider
            return ValueTask.FromResult(false);
        }

        public ValueTask<bool> DeleteTextAsync(int startPos, int endPos)
        {
            if (!TryGetEditable(out var provider, out var current))
                return ValueTask.FromResult(false);

            var start = ToIndex(current, startPos);
            var end = ToIndex(current, endPos);

            if (start >= end)
                return ValueTask.FromResult(false);

            provider.SetValue(current.Remove(start, end - start));
            return ValueTask.FromResult(true);
        }

        public ValueTask<bool> PasteTextAsync(int position)
        {
            // Clipboard operations not supported via IValueProvider
            return ValueTask.FromResult(false);
        }

        private bool TryGetEditable(out IValueProvider provider, out string current)
        {
            current = string.Empty;

            if (node.Peer.GetProvider<IValueProvider>() is not { IsReadOnly: false } valueProvider)
            {
                provider = null!;
                return false;
            }

            provider = valueProvider;

            if (node.TextModel is not { } model)
            {
                current = valueProvider.Value ?? string.Empty;
                return true;
            }

            // AT-SPI addresses the text as drawn. When that differs from the document's own text (a
            // masked password, or a document with several blocks), offsets into one are not offsets
            // into the other and the value cannot be rebuilt from them.
            current = model.Text.GetText(model.Text.DocumentRange);
            return string.Equals(model.PlainText, current, StringComparison.Ordinal);
        }

        private int ToIndex(string current, int offset)
        {
            if (node.TextModel is { } model)
                return Math.Min(model.ToUtf16(offset), current.Length);

            return Math.Clamp(offset, 0, current.Length);
        }
    }
}
