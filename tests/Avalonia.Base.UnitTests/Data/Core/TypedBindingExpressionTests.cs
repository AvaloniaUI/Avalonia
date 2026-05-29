using System;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core;

public class TypedBindingExpressionTests
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
        var source = new ViewModel { Foo = "Hello" };
        var binding = CreateBinding();
        var target = new TextBlock { DataContext = source };

        BindAndAssert(target, binding);

        Assert.Equal("Hello", target.Text);
    }

    [Fact]
    public void Should_Track_String_Value()
    {
        var source = new ViewModel { Foo = "Hello" };
        var binding = CreateBinding();
        var target = new TextBlock { DataContext = source };

        BindAndAssert(target, binding);

        Assert.Equal("Hello", target.Text);

        source.Foo = "World";

        Assert.Equal("World", target.Text);
    }

    [Fact]
    public void Can_Bind_String_To_Object()
    {
        var log = string.Empty;
        using var logger = TestLogSink.Start((_, _, _, m, _) => log += m);
        var source = new ViewModel { Foo = "Hello" };
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
        var source = new ViewModel { Foo = "Hello" };
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

    private static CompiledBinding CreateBinding()
    {
        var propertyInfo = new ClrPropertyInfo<ViewModel, string?>("Foo", v => v.Foo, (o, v) => o.Foo = v);
        var path = new CompiledBindingPathBuilder().Property(propertyInfo).Build();
        return new CompiledBinding(path);
    }

    private class ViewModel : NotifyingBase
    {
        public string? Foo { get; set => SetField(ref field, value); }
    }
}
