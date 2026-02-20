using System;
using System.Threading;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Interactions;
using Xunit;

namespace Avalonia.IntegrationTests.Appium;

[Collection("Default")]
public class DragDropTests : TestBase
{
    public DragDropTests(DefaultAppFixture fixture)
        : base(fixture, "DragDrop")
    {
        var reset = Session.FindElementByAccessibilityId("ResetDragDrop");
        reset.Click();
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void DragDrop_Coordinates_Correct_When_Controls_Offset_From_Origin()
    {
        // This test verifies the fix for drag-drop coordinate calculation when
        // controls are positioned away from the window origin.
        // Issue: In embedded views or when controls have margin/offset from origin,
        // the drag-drop coordinates were incorrectly calculated relative to the
        // window rather than the view.

        var dragSource = Session.FindElementByAccessibilityId("DragSource");
        var dropTarget = Session.FindElementByAccessibilityId("DropTarget");
        var dropPosition = Session.FindElementByAccessibilityId("DropPosition");
        var dragDropStatus = Session.FindElementByAccessibilityId("DragDropStatus");

        // Perform drag from source to target
        new Actions(Session)
            .MoveToElement(dragSource)
            .ClickAndHold()
            .MoveToElement(dropTarget)
            .Release()
            .Perform();

        Thread.Sleep(500); // Allow UI to update

        // Verify the drop was successful
        var status = dragDropStatus.Text;
        Assert.True(status == "Drop OK" || status == "Copied",
            $"Expected drop to succeed, but status was: {status}");

        // Verify the drop position is within the target bounds
        // If the coordinate calculation bug exists, the position would be
        // offset by the spacer/margin and would show negative coordinates
        // or coordinates outside the target bounds
        var positionText = dropPosition.Text;
        Assert.StartsWith("Drop:", positionText, StringComparison.Ordinal);

        // The DropTargetText should not contain "ERROR"
        var dropTargetText = Session.FindElementByAccessibilityId("DropTargetText");
        Assert.DoesNotContain("ERROR", dropTargetText.Text);
    }

    [PlatformFact(TestPlatforms.Linux)]
    public void Linux_DragDrop_Coordinates_Correct_When_Controls_Offset_From_Origin()
    {
        using var fixture = new DefaultAppFixture();
        var isolated = new DragDropTests(fixture);
        isolated.DragDrop_Coordinates_Correct_When_Controls_Offset_From_Origin();
    }

    [PlatformFact(TestPlatforms.MacOS)]
    public void DragDrop_Position_Updates_During_DragOver()
    {
        // Verifies that position is correctly reported during drag-over events
        var dragSource = Session.FindElementByAccessibilityId("DragSource");
        var dropTarget = Session.FindElementByAccessibilityId("DropTarget");
        var dropPosition = Session.FindElementByAccessibilityId("DropPosition");

        // Start drag and move over target, then release
        var device = new PointerInputDevice(PointerKind.Mouse);
        var builder = new ActionBuilder();

        // Move to drag source and start drag
        builder.AddAction(device.CreatePointerMove(dragSource, 0, 0, TimeSpan.FromMilliseconds(100)));
        builder.AddAction(device.CreatePointerDown(MouseButton.Left));

        // Move to drop target (this triggers DragOver events)
        builder.AddAction(device.CreatePointerMove(dropTarget, 0, 0, TimeSpan.FromMilliseconds(200)));

        // Pause to allow DragOver events to be processed
        builder.AddAction(device.CreatePause(TimeSpan.FromMilliseconds(200)));

        // Release at current position (completes the drag)
        builder.AddAction(device.CreatePointerUp(MouseButton.Left));

        Session.PerformActions(builder.ToActionSequenceList());

        Thread.Sleep(200);

        // Check that position was recorded (either DragOver or Drop)
        var positionText = dropPosition.Text;

        // Position should have been updated during drag-over or drop
        Assert.True(positionText.Contains("DragOver:") || positionText.Contains("Drop:"),
            $"Expected position to be updated during drag, but got: {positionText}");
    }

    [PlatformFact(TestPlatforms.Linux)]
    public void Linux_DragDrop_Position_Updates_During_DragOver()
    {
        using var fixture = new DefaultAppFixture();
        var isolated = new DragDropTests(fixture);
        isolated.DragDrop_Position_Updates_During_DragOver();
    }

    [PlatformFact(TestPlatforms.Windows | TestPlatforms.MacOS)]
    public void DragDrop_Can_Be_Cancelled()
    {
        AssertCanBeCancelled(Session);
    }

    [PlatformFact(TestPlatforms.Linux)]
    public void Linux_DragDrop_Can_Be_Cancelled()
    {
        using var fixture = new DefaultAppFixture();
        var isolated = new DragDropTests(fixture);
        AssertCanBeCancelled(isolated.Session);
    }

    private static void AssertCanBeCancelled(AppiumDriver session)
    {
        var offsets = new (int X, int Y)[]
        {
            (-300, -60),
            (-260, 120),
            (-420, 0),
        };

        foreach (var (x, y) in offsets)
        {
            session.FindElementByAccessibilityId("ResetDragDrop").Click();
            if (TryCancelDrag(session, x, y))
                return;
        }

        var finalStatus = GetElementText(session.FindElementByAccessibilityId("DragDropStatus"));
        Assert.Equal("Cancelled", finalStatus);
    }

    private static bool TryCancelDrag(AppiumDriver session, int xOffset, int yOffset)
    {
        var dragSource = session.FindElementByAccessibilityId("DragSource");
        var dragDropStatus = session.FindElementByAccessibilityId("DragDropStatus");

        var device = new PointerInputDevice(PointerKind.Mouse);
        var builder = new ActionBuilder();
        builder.AddAction(device.CreatePointerMove(dragSource, 0, 0, TimeSpan.FromMilliseconds(100)));
        builder.AddAction(device.CreatePointerDown(MouseButton.Left));
        builder.AddAction(device.CreatePause(TimeSpan.FromMilliseconds(100)));
        builder.AddAction(device.CreatePointerMove(dragSource, xOffset, yOffset, TimeSpan.FromMilliseconds(250)));
        builder.AddAction(device.CreatePointerUp(MouseButton.Left));
        session.PerformActions(builder.ToActionSequenceList());

        for (var i = 0; i < 30; ++i)
        {
            if (GetElementText(dragDropStatus) == "Cancelled")
                return true;
            Thread.Sleep(100);
        }

        return false;
    }

    private static string GetElementText(AppiumWebElement element)
    {
        var text = element.Text;
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        text = element.GetAttribute("value");
        if (!string.IsNullOrWhiteSpace(text))
            return text;

        return element.GetAttribute("name") ?? string.Empty;
    }
}
