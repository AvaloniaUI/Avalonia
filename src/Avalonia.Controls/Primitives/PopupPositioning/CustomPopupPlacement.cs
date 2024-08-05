namespace Avalonia.Controls.Primitives.PopupPositioning;

/// <summary>
/// Defines custom placement parameters for a <see cref="CustomPopupPlacementCallback"/> callback.
/// </summary>
public record CustomPopupPlacement
{
    private PopupGravity _gravity;
    private PopupAnchor _anchor;

    internal CustomPopupPlacement(Size popupSize, Visual target)
    {
        PopupSize = popupSize;
        Target = target;
    }

    /// <summary>
    /// The <see cref="Size"/> of the <see cref="Popup"/> control.
    /// </summary>
    public Size PopupSize { get; }

    /// <summary>
    /// Placement target of the popup.
    /// </summary>
    public Visual Target { get; }

    /// <see cref="PopupPositionerParameters.AnchorRectangle"/>
    public Rect AnchorRectangle { get; set; }

    /// <see cref="PopupPositionerParameters.Anchor"/>
    public PopupAnchor Anchor
    {
        get => _anchor;
        set
        {
            PopupPositioningEdgeHelper.ValidateEdge(value);
            _anchor = value;
        }
    }

    /// <see cref="PopupPositionerParameters.Gravity"/>
    public PopupGravity Gravity
    {
        get => _gravity;
        set
        {
            PopupPositioningEdgeHelper.ValidateGravity(value);
            _gravity = value;
        }
    }

    /// <see cref="PopupPositionerParameters.ConstraintAdjustment"/>
    public PopupPositionerConstraintAdjustment ConstraintAdjustment { get; set; }

    /// <see cref="PopupPositionerParameters.Offset"/>
    public Point Offset { get; set; }
}
