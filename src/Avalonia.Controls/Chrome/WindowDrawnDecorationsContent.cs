using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Controls.Chrome;

/// <summary>
/// Holds the template content for <see cref="WindowDrawnDecorations"/>.
/// Contains three visual slots: Underlay, Overlay, and FullscreenPopover.
/// </summary>
public class WindowDrawnDecorationsContent : StyledElement
{
    /// <summary>
    /// Gets or sets the overlay layer content (titlebar, caption buttons).
    /// Positioned above the client area.
    /// </summary>
    public Control? Overlay
    {
        get => field;
        set => HandleLogicalChild(ref field, value);
    }

    /// <summary>
    /// Gets or sets the underlay layer content (borders, background, shadow area).
    /// Positioned below the client area.
    /// </summary>
    public Control? Underlay
    {
        get => field;
        set => HandleLogicalChild(ref field, value);
    }

    /// <summary>
    /// Gets or sets the fullscreen popover content.
    /// Shown when the user hovers the pointer at the top of the window in fullscreen mode.
    /// </summary>
    public Control? FullscreenPopover
    {
        get => field;
        set => HandleLogicalChild(ref field, value);
    }

    private void HandleLogicalChild(ref Control? field, Control? value)
    {
        if (field == value)
            return;
        if (field != null)
        {
            LogicalChildren.Remove(field);
            ((ISetLogicalParent)field).SetParent(null);
        }

        field = value;
        if (field != null)
        {
            LogicalChildren.Add(field);
            ((ISetLogicalParent)field).SetParent(this);
        }
    }
}
