using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class CurvedHeaderHomeScrollView : UserControl
{
    public Action? NavigateRequested { get; set; }

    public CurvedHeaderHomeScrollView() => InitializeComponent();

    void OnShopNowClick(object? sender, RoutedEventArgs e) => NavigateRequested?.Invoke();

    void OnProductClick(object? sender, RoutedEventArgs e) => NavigateRequested?.Invoke();
}
