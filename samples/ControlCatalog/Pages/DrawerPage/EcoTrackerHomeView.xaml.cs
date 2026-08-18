using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages;

public partial class EcoTrackerHomeView : UserControl
{
    public Action? TreeDetailRequested { get; set; }

    public EcoTrackerHomeView() => InitializeComponent();

    void OnHeroClick(object? sender, RoutedEventArgs e) => TreeDetailRequested?.Invoke();
}
