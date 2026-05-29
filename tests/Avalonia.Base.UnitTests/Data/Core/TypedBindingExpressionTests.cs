using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
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

    private class ViewModel
    {
        public string? Foo { get; set; }
    }
}
