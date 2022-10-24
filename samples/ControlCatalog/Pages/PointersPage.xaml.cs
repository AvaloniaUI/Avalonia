using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages;

public class PointersPage : UserControl
{
    public PointersPage()
    {
        this.InitializeComponent();

        var border1 = this.Get<Border>("BorderCapture1");
        var border2 = this.Get<Border>("BorderCapture2");

        border1.PointerPressed += Border_PointerPressed;
        border1.PointerReleased += Border_PointerReleased;
        border1.PointerCaptureLost += Border_PointerCaptureLost;
        border1.PointerMoved += Border_PointerUpdated;
        border1.PointerEntered += Border_PointerUpdated;
        border1.PointerExited += Border_PointerUpdated;

        border2.PointerPressed += Border_PointerPressed;
        border2.PointerReleased += Border_PointerReleased;
        border2.PointerCaptureLost += Border_PointerCaptureLost;
        border2.PointerMoved += Border_PointerUpdated;
        border2.PointerEntered += Border_PointerUpdated;
        border2.PointerExited += Border_PointerUpdated;
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
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
