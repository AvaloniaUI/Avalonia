namespace BuildTests

open Avalonia.Controls
open Avalonia.Markup.Xaml

type MainView() as this =
    inherit UserControl()

    do
        this.InitializeComponent()

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
