using Avalonia.Diagnostics;
using Avalonia.Metadata;

namespace Avalonia.Controls.Primitives.PopupPositioning;

[PrivateApi]
public class PopupPositionRequest
{
    internal PopupPositionRequest(Visual target, PlacementMode placement)
    {
        Target = target;
        Placement = placement;
    }

    internal PopupPositionRequest(Visual target, PlacementMode placement, Point offset, PopupAnchor anchor, PopupGravity gravity, PopupPositionerConstraintAdjustment constraintAdjustment, Rect? anchorRect, CustomPopupPlacementCallback? placementCallback, Point? pointerPosition = null)
        : this(target, placement)
    {
        Offset = offset;
        Anchor = anchor;
        Gravity = gravity;
        ConstraintAdjustment = constraintAdjustment;
        AnchorRect = anchorRect;
        PlacementCallback = placementCallback;
        PointerPosition = pointerPosition;
    }

    public Visual Target { get; }
    public PlacementMode Placement {get;}
    public Point Offset {get;}
    public PopupAnchor Anchor {get;}
    public PopupGravity Gravity {get;}
    public PopupPositionerConstraintAdjustment ConstraintAdjustment {get;}
    public Rect? AnchorRect {get;}
    public CustomPopupPlacementCallback? PlacementCallback {get;}
    internal Point? PointerPosition { get; }
}
