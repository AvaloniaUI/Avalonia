using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class GesturesPage : UserControl
{
    public GesturesPage()
    {
        InitializeComponent();
    }

    private void GestureBorder_Tapped(object? sender, TappedEventArgs e)
    {
        LastGesture.Text = "Tapped";
    }

    private void GestureBorder_DoubleTapped(object? sender, TappedEventArgs e)
    {
        LastGesture.Text = "DoubleTapped";

        // Testing #8733
        GestureBorder.IsVisible = false;
        GestureBorder2.IsVisible = true;
    }

    private void GestureBorder_RightTapped(object? sender, RoutedEventArgs e)
    {
        LastGesture.Text = "RightTapped";
    }

    private void GestureBorder2_DoubleTapped(object? sender, TappedEventArgs e)
    {
        LastGesture.Text = "DoubleTapped2";
    }

    private void ResetGestures_Click(object? sender, RoutedEventArgs e)
    {
        LastGesture.Text = string.Empty;
        GestureBorder.IsVisible = true;
        GestureBorder2.IsVisible = false;
    }
}
