using System.Collections.Generic;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives;

public class ManagedPopupPositionerTests : ScopedTestBase
{
    // The anchor rectangle overlaps the 4 screens, so each corner lands on a different screen.
    // With no gravity the popup is centered on the anchor point and is slid back into the screen containing that point.
    [Theory]
    [InlineData(PopupAnchor.TopLeft, 600, 600)]
    [InlineData(PopupAnchor.TopRight, 1000, 600)]
    [InlineData(PopupAnchor.BottomLeft, 600, 1000)]
    [InlineData(PopupAnchor.BottomRight, 1000, 1000)]
    public void Uses_Screen_Containing_The_Anchor_Point(PopupAnchor anchor, double expectedX, double expectedY)
    {
        var popup = new MockManagedPopupPositionerPopup();
        var positioner = new ManagedPopupPositioner(popup);

        positioner.Update(new PopupPositionerParameters
        {
            Size = new Size(400, 400),
            AnchorRectangle = new Rect(900, 900, 200, 200),
            Anchor = anchor,
            Gravity = PopupGravity.None,
            ConstraintAdjustment = PopupPositionerConstraintAdjustment.All
        });

        Assert.Equal(new Point(expectedX, expectedY), popup.LastPosition);
    }

    private sealed class MockManagedPopupPositionerPopup : IManagedPopupPositionerPopup
    {
        // Four screens arranged in a 2x2 grid, meeting at (1000, 1000).
        public IReadOnlyList<ManagedPopupPositionerScreenInfo> Screens { get; } =
        [
            new(new Rect(0, 0, 1000, 1000), new Rect(0, 0, 1000, 1000)),
            new(new Rect(1000, 0, 1000, 1000), new Rect(1000, 0, 1000, 1000)),
            new(new Rect(0, 1000, 1000, 1000), new Rect(0, 1000, 1000, 1000)),
            new(new Rect(1000, 1000, 1000, 1000), new Rect(1000, 1000, 1000, 1000))
        ];

        public Rect ParentClientAreaScreenGeometry => new(0, 0, 1000, 1000);

        public double Scaling => 1.0;

        public Point LastPosition { get; private set; }

        public Size LastSize { get; private set; }

        public void MoveAndResize(Point devicePoint, Size virtualSize)
        {
            LastPosition = devicePoint;
            LastSize = virtualSize;
        }
    }
}
