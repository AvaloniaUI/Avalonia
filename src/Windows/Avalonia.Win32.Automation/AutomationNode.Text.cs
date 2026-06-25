using System;
using System.Collections.Generic;
using System.Linq;
using AAP = Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.ITextProvider
    {
        UIA.ITextRangeProvider UIA.ITextProvider.GetDocumentRange()
            => new AutomationTextRangeProvider(
                this,
                InvokeSync<AAP.ITextProvider, AAP.ITextRangeProvider>(x => x.DocumentRange));

        UIA.ITextRangeProvider[] UIA.ITextProvider.GetSelection()
            => InvokeSync<AAP.ITextProvider, IReadOnlyList<AAP.ITextRangeProvider>>(x => x.GetSelection())
                .Select(range => (UIA.ITextRangeProvider)new AutomationTextRangeProvider(this, range))
                .ToArray();

        // Deferred: visible ranges (needs viewport).
        UIA.ITextRangeProvider[] UIA.ITextProvider.GetVisibleRanges() => Array.Empty<UIA.ITextRangeProvider>();

        // Deferred: range-from-child (needs embedded-object mapping).
        UIA.ITextRangeProvider UIA.ITextProvider.RangeFromChild(UIA.IRawElementProviderSimple childElement) => null!;

        UIA.ITextRangeProvider UIA.ITextProvider.RangeFromPoint(double x, double y)
        {
            var point = PointFromScreen(x, y);
            var range = InvokeSync<AAP.ITextProvider, AAP.ITextRangeProvider?>(p => p.RangeFromPoint(point));
            return range is null ? null! : new AutomationTextRangeProvider(this, range);
        }

        UIA.SupportedTextSelection UIA.ITextProvider.GetSupportedTextSelection()
            => (UIA.SupportedTextSelection)(int)InvokeSync<AAP.ITextProvider, AAP.SupportedTextSelection>(
                x => x.SupportedTextSelection);
    }
}
