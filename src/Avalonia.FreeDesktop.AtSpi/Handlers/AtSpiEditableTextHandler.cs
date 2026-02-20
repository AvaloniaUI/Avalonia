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
    internal sealed class AtSpiEditableTextHandler : IOrgA11yAtspiEditableText
    {
        private readonly AtSpiNode _node;

        public AtSpiEditableTextHandler(AtSpiServer server, AtSpiNode node)
        {
            _ = server;
            _node = node;
        }

        public uint Version => EditableTextVersion;

        public ValueTask<bool> SetTextContentsAsync(string newContents)
        {
            if (_node.Peer.GetProvider<IValueProvider>() is not { IsReadOnly: false } provider)
                return ValueTask.FromResult(false);

            provider.SetValue(newContents);
            return ValueTask.FromResult(true);

        }

        public ValueTask<bool> InsertTextAsync(int position, string text, int length)
        {
            if (_node.Peer.GetProvider<IValueProvider>() is not { IsReadOnly: false } provider)
                return ValueTask.FromResult(false);
            
            var current = provider.Value ?? string.Empty;
            position = Math.Max(0, Math.Min(position, current.Length));
            var toInsert = length >= 0 && length < text.Length ? text.Substring(0, length) : text;
            var newValue = current.Insert(position, toInsert);
            provider.SetValue(newValue);
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
            if (_node.Peer.GetProvider<IValueProvider>() is not { IsReadOnly: false } provider)
                return ValueTask.FromResult(false);
            
            var current = provider.Value ?? string.Empty;
            startPos = Math.Max(0, Math.Min(startPos, current.Length));
            endPos = Math.Max(startPos, Math.Min(endPos, current.Length));

            if (startPos >= endPos) 
                return ValueTask.FromResult(false);
            var newValue = current.Remove(startPos, endPos - startPos);
            provider.SetValue(newValue);
            return ValueTask.FromResult(true);

        }

        public ValueTask<bool> PasteTextAsync(int position)
        {
            // Clipboard operations not supported via IValueProvider
            return ValueTask.FromResult(false);
        }
    }
}
