using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

namespace Avalonia.Controls;

/// <summary>
/// Hosts the TopLevel and, when enabled, drawn decoration layers (underlay, overlay, fullscreen popover).
/// Serves as the visual root for PresentationSource.
/// </summary>
internal partial class TopLevelHost : Control
{
    private Thickness _decorationInset;

    static TopLevelHost()
    {
        KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TopLevelHost>(KeyboardNavigationMode.Cycle);
    }

    public TopLevelHost(TopLevel tl)
    {
        _topLevel = tl;
        VisualChildren.Add(tl);
    }

    /// <summary>
    /// Gets or sets the decoration inset applied to the TopLevel child in forced decoration mode.
    /// When non-zero, the TopLevel is measured and arranged within the inset area while
    /// decoration layers use the full available size.
    /// </summary>
    internal Thickness DecorationInset
    {
        get => _decorationInset;
        set
        {
            if (_decorationInset == value)
                return;
            _decorationInset = value;
            InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var inset = _decorationInset;
        var hasInset = inset != default;
        var desiredSize = default(Size);

        foreach (var child in VisualChildren)
        {
            if (child is Layoutable l)
            {
                if (hasInset && ReferenceEquals(child, _topLevel))
                {
                    // In forced mode, measure the TopLevel with reduced size
                    var contentSize = new Size(
                        Math.Max(0, availableSize.Width - inset.Left - inset.Right),
                        Math.Max(0, availableSize.Height - inset.Top - inset.Bottom));
                    l.Measure(contentSize);

                    // Add inset back so TopLevelHost's desired size represents the full frame.
                    // This ensures ArrangeOverride receives the full frame size and can correctly
                    // position the TopLevel within the inset area.
                    desiredSize = new Size(
                        Math.Max(desiredSize.Width, l.DesiredSize.Width + inset.Left + inset.Right),
                        Math.Max(desiredSize.Height, l.DesiredSize.Height + inset.Top + inset.Bottom));
                }
                else
                {
                    l.Measure(availableSize);

                    desiredSize = new Size(
                        Math.Max(desiredSize.Width, l.DesiredSize.Width),
                        Math.Max(desiredSize.Height, l.DesiredSize.Height));
                }
            }
        }

        return desiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var inset = _decorationInset;
        var hasInset = inset != default;

        foreach (var child in VisualChildren)
        {
            if (child is Layoutable l)
            {
                if (hasInset && ReferenceEquals(child, _topLevel))
                {
                    // In forced mode, arrange the TopLevel within the inset area
                    var contentSize = new Size(
                        Math.Max(0, finalSize.Width - inset.Left - inset.Right),
                        Math.Max(0, finalSize.Height - inset.Top - inset.Bottom));
                    
                    l.Arrange(new Rect(inset.Left, inset.Top, contentSize.Width, contentSize.Height));
                }
                else
                {
                    l.Arrange(new Rect(finalSize));
                }
            }
        }

        return finalSize;
    }

    protected override bool BypassFlowDirectionPolicies => true;
}
