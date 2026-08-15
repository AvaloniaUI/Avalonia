using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.ITextProvider
    {
        UIA.ITextRangeProvider[] UIA.ITextProvider.GetSelection() =>
            InvokeSync<ITextProvider, IReadOnlyList<ITextRangeProvider>>(x => x.GetSelection())
                .Select(r => (UIA.ITextRangeProvider)new Win32TextRangeProvider(this, r))
                .ToArray();

        UIA.ITextRangeProvider[] UIA.ITextProvider.GetVisibleRanges() =>
            InvokeSync<ITextProvider, IReadOnlyList<ITextRangeProvider>>(x => x.GetVisibleRanges())
                .Select(r => (UIA.ITextRangeProvider)new Win32TextRangeProvider(this, r))
                .ToArray();

        UIA.ITextRangeProvider UIA.ITextProvider.RangeFromChild(UIA.IRawElementProviderSimple childElement)
        {
            // No control implementing ITextProvider today has child text elements (e.g.
            // TextBox), so this is never meaningfully invoked; fall back to the document range,
            // which is never null, to satisfy the non-nullable COM interop contract.
            var range = InvokeSync<ITextProvider, ITextRangeProvider>(x => x.DocumentRange);
            return new Win32TextRangeProvider(this, range);
        }

        UIA.ITextRangeProvider UIA.ITextProvider.RangeFromPoint(UIA.UiaPoint point)
        {
            var range = InvokeSync<ITextProvider, ITextRangeProvider>(t => t.RangeFromPoint(new Point(point.X, point.Y)));
            return new Win32TextRangeProvider(this, range);
        }

        UIA.ITextRangeProvider UIA.ITextProvider.GetDocumentRange()
        {
            var range = InvokeSync<ITextProvider, ITextRangeProvider>(x => x.DocumentRange);
            return new Win32TextRangeProvider(this, range);
        }

        UIA.SupportedTextSelection UIA.ITextProvider.GetSupportedTextSelection() =>
            InvokeSync<ITextProvider, UIA.SupportedTextSelection>(x => (UIA.SupportedTextSelection)x.SupportedTextSelection);
    }
}
