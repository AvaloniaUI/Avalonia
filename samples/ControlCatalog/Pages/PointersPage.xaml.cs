using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace ControlCatalog.Pages;

public partial class PointersPage : UserControl
{
    public PointersPage()
    {
        InitializeComponent();
    }

    private void Border_PointerUpdated(object? sender, PointerEventArgs e)
    {
        if (sender is Border border && border.Child is TextBlock textBlock)
        {
            var position = e.GetPosition(border);
            textBlock.Text = @$"Type: {e.Pointer.Type}
Captured: {e.Pointer.Captured == sender}
PointerId: {e.Pointer.Id}
Position: {(int)position.X} {(int)position.Y}";
            e.Handled = true;
        }
    }

    private void Border_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (sender is Border border && border.Child is TextBlock textBlock)
        {
            textBlock.Text = @$"Type: {e.Pointer.Type}
Captured: {e.Pointer.Captured == sender}
PointerId: {e.Pointer.Id}
Position: ??? ???";
            e.Handled = true;

        }
    }

    private void Border_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Captured == sender)
        {
            e.Pointer.Capture(null);
            e.Handled = true;
        }
        else if (e.Pointer.Captured is not null)
        {
            throw new InvalidOperationException("How?");
        }
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Pointer.Capture(sender as Border);
        e.Handled = true;
        e.PreventGestureRecognition();
    }
}
