using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views;

partial class MultiBindingExpressionView : StackPanel
{
    public MultiBindingExpressionView()
    {
        InitializeComponent();
    }

    protected override Type StyleKeyOverride => typeof(StackPanel);

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
