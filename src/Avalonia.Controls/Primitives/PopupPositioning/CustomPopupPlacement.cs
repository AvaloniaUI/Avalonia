namespace Avalonia.Controls.Primitives.PopupPositioning;

/// <summary>
/// Defines custom placement parameters for a Popup control.
/// </summary>
public record struct CustomPopupPlacement
{
    private PopupGravity _gravity;
    private PopupAnchor _anchor;

    /// <see cref="PopupPositionerParameters.Anchor"/>
    public PopupAnchor Anchor
    {
        get => _anchor;
        init
        {
            PopupPositioningEdgeHelper.ValidateEdge(value);
            _anchor = value;
        }
    }

    /// <see cref="PopupPositionerParameters.Gravity"/>
    public PopupGravity Gravity
    {
        get => _gravity;
        init
        {
            PopupPositioningEdgeHelper.ValidateGravity(value);
            _gravity = value;
        }
    }

    /// <see cref="PopupPositionerParameters.ConstraintAdjustment"/>
    public PopupPositionerConstraintAdjustment ConstraintAdjustment { get; init; }

    /// <see cref="PopupPositionerParameters.Offset"/>
    public Point Offset { get; init; }
}
