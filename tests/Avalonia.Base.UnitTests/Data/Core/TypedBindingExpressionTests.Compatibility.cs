using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;
using Avalonia.Logging;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core;

/// <summary>
/// Tests which compare the behaviour of <see cref="TypedBindingExpression{TSource, TValue}"/> with
/// the untyped <see cref="BindingExpression"/> for bindings which are eligible for the typed path.
/// </summary>
/// <remarks>
/// Each test is run twice: once with a binding which produces a typed expression and once with an
/// equivalent binding which produces an untyped expression. The assertions describe the behaviour
/// of the untyped expression, i.e. the behaviour of the binding before typed binding expressions
/// were introduced, so a failure in the <c>typed: true</c> case is a user-visible breaking change.
/// </remarks>
public partial class TypedBindingExpressionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Null_DataContext_Should_Not_Override_Style_Setter(bool typed)
    {
        // A binding which has no value must not contribute a value to the target property,
        // otherwise the property's default value is applied at LocalValue priority, hiding the
        // value from the style setter.
        var target = new TextBlock();
        var root = new TestRoot
        {
            Styles =
            {
                new Style(x => x.OfType<TextBlock>())
                {
                    Setters = { new Setter(TextBlock.TextProperty, "styled") },
                },
            },
            Child = target
        };

        AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

        Assert.Equal("styled", target.Text);
        Assert.Equal(BindingPriority.Style, target.GetDiagnostic(TextBlock.TextProperty).Priority);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Null_DataContext_Should_Not_Break_Property_Inheritance(bool typed)
    {
        // As above, but for an inherited property: applying the property's default value at
        // LocalValue priority stops the value being inherited from the parent.
        var target = new TextBlock();
        var root = new TestRoot
        {
            Child = target,
            [TextBlock.FontSizeProperty] = 30.0,
        };

        AssertExpressionType(typed, target.Bind(TextBlock.FontSizeProperty, CreateDoubleBinding(typed)));

        Assert.Equal(30.0, target.FontSize);
        Assert.Equal(BindingPriority.Inherited, target.GetDiagnostic(TextBlock.FontSizeProperty).Priority);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Style_Priority_Binding_With_Null_DataContext_Should_Not_Break_Property_Inheritance(bool typed)
    {
        // The same problem occurs for bindings at a priority other than LocalValue, such as a
        // binding in a style setter.
        var target = new TextBlock();
        var root = new TestRoot
        {
            Child = target,
            [TextBlock.FontSizeProperty] = 30.0,
        };

        var binding = CreateDoubleBinding(typed);
        binding.Priority = BindingPriority.Style;

        AssertExpressionType(typed, target.Bind(TextBlock.FontSizeProperty, binding));

        Assert.Equal(30.0, target.FontSize);
        Assert.Equal(BindingPriority.Inherited, target.GetDiagnostic(TextBlock.FontSizeProperty).Priority);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Setting_Object_Target_Property_To_A_Different_Type_Should_Not_Throw(bool typed)
    {
        // The binding value type only needs to be assignable to the target property type, so a
        // string can be bound to an object-typed property. Writing a value of any other type to
        // that property must not throw when the binding reads the new target value.
        var data = new ViewModel { StringValue = "foo" };
        var target = new TextBlock { DataContext = data };
        var root = new TestRoot
        {
            Child = target
        };

        AssertExpressionType(typed, target.Bind(TextBlock.TagProperty, CreateStringBinding(typed)));

        Assert.Equal("foo", target.Tag);

        var ex = Record.Exception(() => target.Tag = 5);

        Assert.Null(ex);
        Assert.Equal(5, target.Tag);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Setting_Object_Target_Property_To_Null_Should_Not_Throw(bool typed)
    {
        // As above, but with a value-typed binding: writing null to the object-typed target
        // property must not throw when the binding reads the new target value.
        var data = new ViewModel { DoubleValue = 1.0 };
        var target = new TextBlock { DataContext = data };
        var root = new TestRoot
        {
            Child = target
        };

        AssertExpressionType(typed, target.Bind(TextBlock.TagProperty, CreateDoubleBinding(typed)));

        Assert.Equal(1.0, target.Tag);

        var ex = Record.Exception(() => target.SetValue(TextBlock.TagProperty, null));

        Assert.Null(ex);
        Assert.Null(target.Tag);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Incompatible_DataContext_Should_Log_A_Binding_Error(bool typed)
    {
        // When the DataContext isn't of the expected type the untyped expression logs a binding
        // error; the typed expression silently produces no value.
        var errors = new List<string>();

        using var sink = TestLogSink.Start((level, area, source, template, values) =>
        {
            if (level >= LogEventLevel.Warning && area == LogArea.Binding)
                errors.Add(template);
        });

        var target = new TextBlock { DataContext = new ViewModel { StringValue = "foo" } };
        var root = new TestRoot
        {
            Child = target
        };

        AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

        Assert.Equal("foo", target.Text);

        target.DataContext = new object();

        Assert.NotEmpty(errors);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Source_Getter_Exception_Should_Clear_The_Target_Value(bool typed)
    {
        // When the source getter throws, the untyped expression reports a binding error and
        // reverts the target to its default value; the typed expression silently leaves the stale
        // value in place.
        var errors = new List<string>();

        using var sink = TestLogSink.Start((level, area, source, template, values) =>
        {
            if (level >= LogEventLevel.Warning && area == LogArea.Binding)
                errors.Add(template);
        });

        var data = new ViewModel { StringValue = "foo" };
        var target = new TextBlock { DataContext = data };
        var root = new TestRoot
        {
            Child = target
        };

        AssertExpressionType(typed, target.Bind(TextBlock.TextProperty, CreateStringBinding(typed)));

        Assert.Equal("foo", target.Text);

        data.ThrowOnGet = true;
        data.RaisePropertyChanged(nameof(ViewModel.StringValue));

        Assert.Null(target.Text);
        Assert.NotEmpty(errors);
    }

    private static void AssertExpressionType(bool typed, BindingExpressionBase expression)
    {
        if (typed)
            Assert.IsNotType<BindingExpression>(expression);
        else
            Assert.IsType<BindingExpression>(expression);
    }

    private static CompiledBinding CreateStringBinding(bool typed, BindingMode mode = BindingMode.OneWay)
    {
        if (typed)
            return CreateBinding(mode);

        var path = new CompiledBindingPathBuilder().Property(
            new ClrPropertyInfo(
                nameof(ViewModel.StringValue),
                o => ((ViewModel)o).StringValue,
                (o, v) => ((ViewModel)o).StringValue = (string?)v,
                typeof(string)),
            PropertyInfoAccessorFactory.CreateInpcPropertyAccessor).Build();

        return new CompiledBinding(path) { Mode = mode };
    }

    private static CompiledBinding CreateDoubleBinding(bool typed, BindingMode mode = BindingMode.OneWay)
    {
        var builder = new CompiledBindingPathBuilder();

        if (typed)
        {
            builder.Property<ViewModel, double>(
                new ClrPropertyInfo<ViewModel, double>(
                    nameof(ViewModel.DoubleValue),
                    o => o.DoubleValue,
                    (o, v) => o.DoubleValue = v),
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor,
                false);
        }
        else
        {
            builder.Property(
                new ClrPropertyInfo(
                    nameof(ViewModel.DoubleValue),
                    o => ((ViewModel)o).DoubleValue,
                    (o, v) => ((ViewModel)o).DoubleValue = (double)v!,
                    typeof(double)),
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor);
        }

        return new CompiledBinding(builder.Build()) { Mode = mode };
    }
}
