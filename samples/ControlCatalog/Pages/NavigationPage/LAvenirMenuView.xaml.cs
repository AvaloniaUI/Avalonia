using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace ControlCatalog.Pages;

public partial class LAvenirMenuView : UserControl
{
    public Action<string, string, string, string>? DishSelected { get; set; }

    public LAvenirMenuView() => InitializeComponent();

    void OnDish1Pressed(object? sender, PointerPressedEventArgs e) =>
        DishSelected?.Invoke("Seared Scallops", "$38",
            "Fresh scallops with truffle butter and microgreens", "dish1.jpg");

    void OnDish2Pressed(object? sender, PointerPressedEventArgs e) =>
        DishSelected?.Invoke("Truffle Risotto", "$34",
            "Creamy arborio rice with black truffle shavings", "dish2.jpg");

    void OnDish3Pressed(object? sender, PointerPressedEventArgs e) =>
        DishSelected?.Invoke("Wagyu Tartare", "$42",
            "Hand-cut wagyu beef with quail egg yolk", "dish3.jpg");

    void OnDish4Pressed(object? sender, PointerPressedEventArgs e) =>
        DishSelected?.Invoke("Lobster Bisque", "$24",
            "Classic French bisque with cream and cognac", "dish4.jpg");
}
