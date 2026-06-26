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

        UIA.ITextRangeProvider[] UIA.ITextProvider.GetVisibleRanges()
            => InvokeSync<AAP.ITextProvider, IReadOnlyList<AAP.ITextRangeProvider>>(x => x.GetVisibleRanges())
                .Select(range => (UIA.ITextRangeProvider)new AutomationTextRangeProvider(this, range))
                .ToArray();

        UIA.ITextRangeProvider UIA.ITextProvider.RangeFromChild(UIA.IRawElementProviderSimple childElement)
        {
            if (childElement is not AutomationNode node)
                return null!;

            var range = InvokeSync<AAP.ITextProvider, AAP.ITextRangeProvider?>(p => p.RangeFromChild(node.Peer));
            return range is null ? null! : new AutomationTextRangeProvider(this, range);
        }

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
