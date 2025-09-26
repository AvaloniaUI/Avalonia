#nullable enable

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class AssignBindingTests : XamlTestBase
{
    [Fact]
    public void AssignBinding_Works_With_Clr_Property()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var control = (AssignBindingTestControl)AvaloniaRuntimeXamlLoader.Load(
            """
            <local:AssignBindingTestControl
                xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
                ClrBinding='{Binding SomePath}' />
            """);

        Assert.NotNull(control.ClrBinding);
    }

    [Fact]
    public void AssignBinding_Works_With_AttachedProperty()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var control = (Control)AvaloniaRuntimeXamlLoader.Load(
            """
            <Control
                xmlns='https://github.com/avaloniaui'
                xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                xmlns:local='clr-namespace:Avalonia.Markup.Xaml.UnitTests.Xaml;assembly=Avalonia.Markup.Xaml.UnitTests'
                local:AssignBindingTestControl.AttachedBinding='{Binding SomePath}' />
            """);

        var binding = AssignBindingTestControl.GetAttachedBinding(control);
        Assert.NotNull(binding);
    }
}

public sealed class AssignBindingTestControl : Control
{
    [AssignBinding]
    public BindingBase? ClrBinding { get; set; }

    public static readonly AttachedProperty<BindingBase?> AttachedBindingProperty =
        AvaloniaProperty.RegisterAttached<AssignBindingTestControl, Control, BindingBase?>("AttachedBinding");

    [AssignBinding]
    public static BindingBase? GetAttachedBinding(Control obj)
        => obj.GetValue(AttachedBindingProperty);

    public static void SetAttachedBinding(Control obj, BindingBase? value)
        => obj.SetValue(AttachedBindingProperty, value);
}
