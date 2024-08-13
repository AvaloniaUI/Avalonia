using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace Avalonia.Diagnostics.Views;

partial class BindingExpressionView : StackPanel
{
    public BindingExpressionView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override Type StyleKeyOverride => typeof(StackPanel);
}
