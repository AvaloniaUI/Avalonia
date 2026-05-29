using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core;

public class TypedBindingExpressionTests : ScopedTestBase
{
    [Fact]
    public void Should_Produce_TypedBindingExpression()
    {
        var binding = CreateBinding();
        var target = new TextBlock();
        
        BindAndAssert(target, binding);
    }

    [Fact]
    public void Should_Bind_String_Value()
    {
        var data = new ViewModel { StringValue = "Hello" };
        var target = CreateTarget(data);

        Assert.Equal("Hello", target.Text);
    }

    [Fact]
    public void OneWay_Binding_Should_Track_String_Value()
    {
        var data = new ViewModel { StringValue = "Hello" };
        var target = CreateTarget(data, mode: BindingMode.OneWay);

        Assert.Equal("Hello", target.Text);

        data.StringValue = "World";

        Assert.Equal("World", target.Text);
    }

    [Fact]
    public void OneWay_Binding_Should_Track_DataContext()
    {
        var data1 = new ViewModel { StringValue = "Hello" };
        var data2 = new ViewModel { StringValue = "World" };
        var target = CreateTarget(data1, mode: BindingMode.OneWay);

        Assert.Equal("Hello", target.Text);

        target.DataContext = data2;

        Assert.Equal("World", target.Text);
    }

    // The name of this test makes no sense in English but keeping it as it matches the name of
    // the test in BindingExpressionTests.
    [Fact]
    public void OneWay_Binding_Updates_Target_When_Changes_And_Source_Raises_PropertyChanged()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTarget(data, mode: BindingMode.OneWay);

        Assert.Equal("foo", target.Text);

        target.SetCurrentValue(TextBox.TextProperty, "bar");
        
        Assert.Equal("bar", target.Text);

        data.RaisePropertyChanged(nameof(data.StringValue));

        Assert.Equal("foo", target.Text);
    }

    [Fact]
    public void TwoWay_Binding_Writes_Value_To_Source()
    {
        var source = new ViewModel { StringValue = "Hello" };
        var target = CreateTarget(source, mode: BindingMode.TwoWay);

        Assert.Equal("Hello", target.Text);

        source.StringValue = "World";

        Assert.Equal("World", target.Text);

        target.Text = "Goodbye";

        Assert.Equal("Goodbye", source.StringValue);
    }

    [Fact]
    public void OneTime_Binding_Sets_Target_Only_Once_If_Data_Context_Does_Not_Change()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTarget(data, mode: BindingMode.OneTime);

        Assert.Equal("foo", target.Text);

        data.StringValue = "bar";

        Assert.Equal("foo", target.Text);
    }

    [Fact]
    public void OneTime_Binding_Sets_Target_When_Data_Context_Changes()
    {
        var data = new ViewModel { StringValue = "foo" };
        var target = CreateTarget(data, mode: BindingMode.OneTime);

        Assert.Equal("foo", target.Text);

        target.DataContext = new ViewModel { StringValue = "bar" };

        Assert.Equal("bar", target.Text);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext()
    {
        var target = CreateTarget(null, mode: BindingMode.OneTime);

        Assert.Null(target.Text);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Name()
    {
        var data1 = new { Baz = "baz" };
        var data2 = new ViewModel { StringValue = "foo" };
        var target = CreateTarget(null, mode: BindingMode.OneTime);

        target.DataContext = data1;
        Assert.Null(target.Text);

        target.DataContext = data2;
        Assert.Equal("foo", target.Text);

        data2.StringValue = "bar";
        Assert.Equal("foo", target.Text);
    }

    [Fact]
    public void OneTime_Binding_Waits_For_DataContext_With_Matching_Property_Type()
    {
        var data1 = new { StringValue = 1.5 };
        var data2 = new ViewModel { StringValue = "foo" };
        var target = CreateTarget(null, mode: BindingMode.OneTime);

        target.DataContext = data1;
        Assert.Null(target.Text);

        target.DataContext = data2;
        Assert.Equal("foo", target.Text);

        data2.StringValue = "bar";
        Assert.Equal("foo", target.Text);
    }

    [Fact]
    public void OneWayToSource_Binding_Updates_Source_When_Target_Changes()
    {
        var data = new ViewModel();
        var target = CreateTarget(data, mode: BindingMode.OneWayToSource);

        Assert.Null(data.StringValue);

        target.Text = "foo";
        Assert.Equal("foo", data.StringValue);
    }

    [Fact]
    public void OneWayToSource_Binding_Does_Not_Update_Target_When_Source_Changes()
    {
        var data = new ViewModel();
        var target = CreateTarget(data, mode: BindingMode.OneWayToSource);

        target.Text = "foo";
        Assert.Equal("foo", data.StringValue);

        data.StringValue = "bar";
        Assert.Equal("foo", target.Text);
    }

    [Fact]
    public void OneWayToSource_Binding_Updates_Source_When_DataContext_Changes()
    {
        var data1 = new ViewModel();
        var data2 = new ViewModel();
        var target = CreateTarget(data1, mode: BindingMode.OneWayToSource);

        target.Text = "foo";
        Assert.Equal("foo", data1.StringValue);

        target.DataContext = data2;
        Assert.Equal("foo", data2.StringValue);
    }

    [Fact]
    public void Can_Bind_Readonly_Property_OneWayToSource()
    {
        var data = new ViewModel();
        var target = new SelectableTextBlock
        {
            DataContext = data,
            Text = "foobar",
            SelectionStart = 0,
            SelectionEnd = 3
        };

        Assert.Equal("foo", target.SelectedText);

        var binding = CreateBinding(mode: BindingMode.OneWayToSource);
        target.Bind(SelectableTextBlock.SelectedTextProperty, binding);

        Assert.Equal("foo", data.StringValue);

        target.SelectionEnd = 4;

        // TODO: Uncomment when https://github.com/AvaloniaUI/Avalonia/issues/21461 fixed.
        //Assert.Equal("foob", data.StringValue);
    }

    [Fact]
    public void Can_Bind_String_To_Object()
    {
        var log = string.Empty;
        using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
        var source = new ViewModel { StringValue = "Hello" };
        var binding = CreateBinding();
        var target = new TextBlock { DataContext = source };
        var expression = target.Bind(TextBlock.TagProperty, binding);

        Assert.IsType<TypedBindingExpression<ViewModel, string?>>(expression);
    }

    [Fact]
    public void Should_Throw_When_Binding_String_To_Double()
    {
        var log = string.Empty;
        using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
        var source = new ViewModel { StringValue = "Hello" };
        var binding = CreateBinding();
        var target = new TextBlock { DataContext = source };
        var exception = Assert.Throws<InvalidOperationException>(() => 
            target.Bind(TextBlock.OpacityProperty, binding));
    }

    private static TypedBindingExpression<ViewModel, string?> BindAndAssert(StyledElement target, BindingBase binding)
    {
        var expression = target.Bind(TextBlock.TextProperty, binding);
        return Assert.IsType<TypedBindingExpression<ViewModel, string?>>(expression);
    }

    private static CompiledBinding CreateBinding(BindingMode mode = BindingMode.OneWay)
    {
        var propertyInfo = new ClrPropertyInfo<ViewModel, string?>(
            nameof(ViewModel.StringValue),
            v => v.StringValue,
            (o, v) => o.StringValue = v);
        var path = new CompiledBindingPathBuilder().Property(propertyInfo).Build();
        return new CompiledBinding(path) { Mode = mode, };
    }

    private static TextBox CreateTarget(ViewModel? data, BindingMode mode = BindingMode.OneWay)
    {
        var result = new TextBox { DataContext = data };
        var binding = CreateBinding(mode);
        BindAndAssert(result, binding);
        return result;
    }

    private class ViewModel : NotifyingBase
    {
        public string? StringValue { get; set => SetField(ref field, value); }
    }
}
