using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data;

public class BindingOperationsTests
{
    [Fact]
    public void GetBindingExpressionBase_Returns_Null_When_Not_Bound()
    {
        var target = new Control();
        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.Null(expression);
    }

    [Theory]
    [InlineData(BindingPriority.Animation)]
    [InlineData(BindingPriority.LocalValue)]
    [InlineData(BindingPriority.Style)]
    [InlineData(BindingPriority.StyleTrigger)]
    public void GetBindingExpressionBase_Returns_Expression_When_Bound(BindingPriority priority)
    {
        var data = new { Tag = "foo" };
        var target = new Control { DataContext = data };
        var binding = new Binding("Tag") { Priority = priority };
        target.Bind(Control.TagProperty, binding);

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Expression_When_Bound_Locally_With_Binding_Error()
    {
        // Target has no data context so binding will fail.
        var target = new Control();
        var binding = new Binding("Tag");
        target.Bind(Control.TagProperty, binding);

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Expression_When_Bound_To_MultiBinding()
    {
        var data = new { Tag = "foo" };
        var target = new Control { DataContext = data };
        var binding = new MultiBinding
        {
            Converter = new FuncMultiValueConverter<object, string>(x => string.Join(',', x)),
            Bindings =
            {
                new Binding("Tag"),
                new Binding("Tag"),
            }
        };

        target.Bind(Control.TagProperty, binding);

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Binding_When_Bound_Via_ControlTheme()
    {
        var target = new Control();
        var binding = new Binding("Tag");
        var theme = new ControlTheme(typeof(Control))
        {
            Setters = { new Setter(Control.TagProperty, binding) },
        };

        target.Theme = theme;
        var root = new TestRoot(target);
        root.UpdateLayout();

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Binding_When_Bound_Via_ControlTheme_TemplateBinding()
    {
        var target = new Control();
        var binding = new TemplateBinding(Control.TagProperty);
        var theme = new ControlTheme(typeof(Control))
        {
            Setters = { new Setter(Control.TagProperty, binding) },
        };

        target.Theme = theme;
        var root = new TestRoot(target);
        root.UpdateLayout();

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Binding_When_Bound_Via_ControlTheme_Style()
    {
        var target = new Control { Classes = { "foo" } };
        var binding = new Binding("Tag");
        var theme = new ControlTheme(typeof(Control))
        {
            Children =
            {
                new Style(x => x.Nesting().Class("foo"))
                {
                    Setters = { new Setter(Control.TagProperty, binding) },
                },
            }
        };

        target.Theme = theme;
        var root = new TestRoot(target);
        root.UpdateLayout();

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }

    [Fact]
    public void GetBindingExpressionBase_Returns_Binding_When_Bound_Via_Style()
    {
        var target = new Control();
        var binding = new Binding("Tag");
        var style = new Style(x => x.OfType<Control>())
        {
            Setters = { new Setter(Control.TagProperty, binding) },
        };

        var root = new TestRoot();
        root.Styles.Add(style);
        root.Child = target;
        root.UpdateLayout();

        var expression = BindingOperations.GetBindingExpressionBase(target, Control.TagProperty);
        Assert.NotNull(expression);
    }
}
