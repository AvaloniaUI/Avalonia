using System;
using System.Collections.Generic;
using System.Linq;
using AAP = Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.ITextProvider, UIA.ITextProvider2
    {
        // The flattened ITextProvider2 repeats the base members; forward them to the ITextProvider
        // implementations below.
        UIA.ITextRangeProvider[] UIA.ITextProvider2.GetSelection() => ((UIA.ITextProvider)this).GetSelection();
        UIA.ITextRangeProvider[] UIA.ITextProvider2.GetVisibleRanges() => ((UIA.ITextProvider)this).GetVisibleRanges();
        UIA.ITextRangeProvider UIA.ITextProvider2.RangeFromChild(UIA.IRawElementProviderSimple childElement)
            => ((UIA.ITextProvider)this).RangeFromChild(childElement);
        UIA.ITextRangeProvider UIA.ITextProvider2.RangeFromPoint(double x, double y)
            => ((UIA.ITextProvider)this).RangeFromPoint(x, y);
        UIA.ITextRangeProvider UIA.ITextProvider2.GetDocumentRange() => ((UIA.ITextProvider)this).GetDocumentRange();
        UIA.SupportedTextSelection UIA.ITextProvider2.GetSupportedTextSelection()
            => ((UIA.ITextProvider)this).GetSupportedTextSelection();

        // No annotation support; TextPattern2 is offered for the caret.
        UIA.ITextRangeProvider? UIA.ITextProvider2.RangeFromAnnotation(UIA.IRawElementProviderSimple? annotationElement)
            => null;

        UIA.ITextRangeProvider? UIA.ITextProvider2.GetCaretRange(out bool isActive)
        {
            var (active, range) = InvokeSync<AAP.ITextProvider2, (bool, AAP.ITextRangeProvider?)>(p =>
            {
                var caret = p.GetCaretRange(out var a);
                return (a, caret);
            });

            isActive = active;
            return range is null ? null : new AutomationTextRangeProvider(this, range);
        }

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
