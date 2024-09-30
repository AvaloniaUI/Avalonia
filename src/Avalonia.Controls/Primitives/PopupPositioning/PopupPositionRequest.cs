using Avalonia.Diagnostics;
using Avalonia.Metadata;

namespace Avalonia.Controls.Primitives.PopupPositioning;

[PrivateApi]
[Unstable(ObsoletionMessages.MayBeRemovedInAvalonia12)]
public class PopupPositionRequest
{
    internal PopupPositionRequest(Visual target, PlacementMode placement)
    {
        Target = target;
        Placement = placement;
    }

    internal PopupPositionRequest(Visual target, PlacementMode placement, Point offset, PopupAnchor anchor, PopupGravity gravity, PopupPositionerConstraintAdjustment constraintAdjustment, Rect? anchorRect, CustomPopupPlacementCallback? placementCallback)
        : this(target, placement)
    {
        Offset = offset;
        Anchor = anchor;
        Gravity = gravity;
        ConstraintAdjustment = constraintAdjustment;
        AnchorRect = anchorRect;
        PlacementCallback = placementCallback;
    }

    public Visual Target { get; }
    public PlacementMode Placement {get;}
    public Point Offset {get;}
    public PopupAnchor Anchor {get;}
    public PopupGravity Gravity {get;}
    public PopupPositionerConstraintAdjustment ConstraintAdjustment {get;}
    public Rect? AnchorRect {get;}
    public CustomPopupPlacementCallback? PlacementCallback {get;}
}
