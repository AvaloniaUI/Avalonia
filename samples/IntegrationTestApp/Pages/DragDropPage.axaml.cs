using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class DragDropPage : UserControl
{
    public DragDropPage()
    {
        InitializeComponent();

        // Set up drag-drop event handlers
        AddHandler(DragDrop.DragOverEvent, DropTarget_DragOver);
        AddHandler(DragDrop.DropEvent, DropTarget_Drop);
    }

    private async void DragSource_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var dragData = new DataTransfer();
            dragData.Add(DataTransferItem.CreateText("TestDragData"));

            DragDropStatus.Text = "Dragging...";

            var result = await DragDrop.DoDragDropAsync(e, dragData, DragDropEffects.Copy | DragDropEffects.Move);

            DragDropStatus.Text = result switch
            {
                DragDropEffects.Copy => "Copied",
                DragDropEffects.Move => "Moved",
                DragDropEffects.None => "Cancelled",
                _ => $"Result: {result}"
            };
        }
    }

    private void DropTarget_DragOver(object? sender, DragEventArgs e)
    {
        // Only handle events for the drop target
        if (e.Source != DropTarget && !IsChildOf(e.Source as Visual, DropTarget))
            return;

        e.DragEffects = DragDropEffects.Copy;

        // Get the position relative to the drop target
        var position = e.GetPosition(DropTarget);
        DropPosition.Text = $"DragOver: ({position.X:F0}, {position.Y:F0})";
    }

    private void DropTarget_Drop(object? sender, DragEventArgs e)
    {
        // Only handle events for the drop target
        if (e.Source != DropTarget && !IsChildOf(e.Source as Visual, DropTarget))
            return;

        // Get the position relative to the drop target
        var position = e.GetPosition(DropTarget);
        DropPosition.Text = $"Drop: ({position.X:F0}, {position.Y:F0})";

        // Check if the position is within reasonable bounds of the drop target
        var bounds = DropTarget.Bounds;
        var isWithinBounds = position.X >= 0 && position.X <= bounds.Width &&
                             position.Y >= 0 && position.Y <= bounds.Height;

        var text = e.DataTransfer.TryGetText();
        if (text != null)
        {
            DropTargetText.Text = isWithinBounds
                ? $"Dropped: {text} at ({position.X:F0}, {position.Y:F0})"
                : $"ERROR: Position out of bounds! ({position.X:F0}, {position.Y:F0})";
            DragDropStatus.Text = isWithinBounds ? "Drop OK" : "Drop position ERROR";
        }

        e.DragEffects = DragDropEffects.Copy;
    }

    private static bool IsChildOf(Visual? child, Visual? parent)
    {
        if (child == null || parent == null)
            return false;

        var current = child.Parent as Visual;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.Parent as Visual;
        }
        return false;
    }

    private void ResetDragDrop_Click(object? sender, RoutedEventArgs e)
    {
        DropPosition.Text = string.Empty;
        DragDropStatus.Text = string.Empty;
        DropTargetText.Text = "Drop items here";
    }
}
