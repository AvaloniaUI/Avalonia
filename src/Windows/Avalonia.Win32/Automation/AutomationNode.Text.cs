using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.ITextProvider
    {
        public UIA.ITextRangeProvider DocumentRange
        {
            get
            {
                var range = InvokeSync<ITextProvider, TextRange>(x => x.DocumentRange);
                return new AutomationTextRange(this, range);
            }
        }

        public UIA.SupportedTextSelection SupportedTextSelection
        {
            get => (UIA.SupportedTextSelection)InvokeSync<ITextProvider, SupportedTextSelection>(x => x.SupportedTextSelection);
        }

        public UIA.ITextRangeProvider[] GetVisibleRanges() => new[] { DocumentRange };

        public UIA.ITextRangeProvider? RangeFromChild(UIA.IRawElementProviderSimple childElement) => null;

        public UIA.ITextRangeProvider? RangeFromPoint(Point screenLocation)
        {
            var p = ToClient(screenLocation);
            var range = InvokeSync<ITextProvider, TextRange>(x => x.RangeFromPoint(screenLocation));
            return new AutomationTextRange(this, range);
        }

        UIA.ITextRangeProvider[] UIA.ITextProvider.GetSelection() => GetRanges(x => x.GetSelection());

        private UIA.ITextRangeProvider[] GetRanges(Func<ITextProvider, IReadOnlyList<TextRange>> selector)
        {
            var source = InvokeSync(selector);
            return source?.Select(x => new AutomationTextRange(this, x)).ToArray() ?? Array.Empty<AutomationTextRange>();
        }

        private void InitializeTextProvider()
        {
            if (Peer is ITextProvider provider)
            {
                provider.SelectedRangesChanged += PeerSelectedTextRangesChanged;
                provider.TextChanged += PeerTextChanged;
            }
        }

        private void PeerSelectedTextRangesChanged(object? sender, EventArgs e)
        {
            RaiseEvent(UIA.UiaEventId.Text_TextSelectionChanged);
        }

        private void PeerTextChanged(object? sender, EventArgs e)
        {
            RaiseEvent(UIA.UiaEventId.Text_TextChanged);
        }
    }
}
