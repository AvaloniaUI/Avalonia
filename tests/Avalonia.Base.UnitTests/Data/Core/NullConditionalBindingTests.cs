using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core;

/// <summary>
/// Tests for null-conditional operator in binding paths.
/// </summary>
/// <remarks>
/// Ideally these would be part of the <see cref="BindingExpressionTests"/> suite but that uses
/// C# expression trees as an abstraction to represent both reflection and compiled binding paths.
/// This is a problem because expression trees don't support the C# null-conditional operator
/// and I have no desire to refactor all of those tests right now.
/// </remarks>
public class NullConditionalBindingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Report_Error_Without_Null_Conditional_Operator(bool compileBindings)
    {
        // Testing the baseline: should report a null error without null conditionals.
        using var app = Start();
        using var log = TestLogger.Create();

        var xaml = $$$"""
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='using:Avalonia.Base.UnitTests.Data.Core'
                    x:DataType='local:NullConditionalBindingTests+First'
                    x:CompileBindings='{{{compileBindings}}}'>
                <local:ErrorCollectingTextBox Text='{Binding Second.Third.Final}'/>
            </Window>
            """;
        var data = new First(new Second(null));
        var window = CreateTarget(xaml, data);
        var textBox = Assert.IsType<ErrorCollectingTextBox>(window.Content);
        var error = Assert.IsType<BindingChainException>(textBox.Error);
        var message = Assert.Single(log.Messages);

        Assert.Null(textBox.Text);
        Assert.Equal("Second.Third.Final", error.Expression);
        Assert.Equal("Third", error.ExpressionErrorPoint);
        Assert.Equal(BindingValueType.BindingError, textBox.ErrorState);
        Assert.Equal("An error occurred binding {Property} to {Expression} at {ExpressionErrorPoint}: {Message}", message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Not_Report_Error_With_Null_Conditional_Operator(bool compileBindings)
    {
        using var app = Start();
        using var log = TestLogger.Create();
        var xaml = $$$"""
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='using:Avalonia.Base.UnitTests.Data.Core'
                    x:DataType='local:NullConditionalBindingTests+First'
                    x:CompileBindings='{{{compileBindings}}}'>
                <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Final}'/>
            </Window>
            """;
        var data = new First(new Second(null));
        var window = CreateTarget(xaml, data);
        var textBox = Assert.IsType<ErrorCollectingTextBox>(window.Content);

        Assert.Null(textBox.Text);
        Assert.Null(textBox.Error);
        Assert.Equal(BindingValueType.Value, textBox.ErrorState);
        Assert.Empty(log.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Not_Report_Error_With_Null_Conditional_Operator_Before_Method(bool compileBindings)
    {
        using var app = Start();
        using var log = TestLogger.Create();
        var xaml = $$$"""
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='using:Avalonia.Base.UnitTests.Data.Core'
                    x:DataType='local:NullConditionalBindingTests+First'
                    x:CompileBindings='{{{compileBindings}}}'>
                <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Greeting}'/>
            </Window>
            """;
        var data = new First(new Second(null));
        var window = CreateTarget(xaml, data);
        var textBox = Assert.IsType<ErrorCollectingTextBox>(window.Content);

        Assert.Null(textBox.Text);
        Assert.Null(textBox.Error);
        Assert.Equal(BindingValueType.Value, textBox.ErrorState);
        Assert.Empty(log.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Should_Use_TargetNullValue_With_Null_Conditional_Operator(bool compileBindings)
    {
        using var app = Start();
        using var log = TestLogger.Create();
        var xaml = $$$"""
            <Window xmlns='https://github.com/avaloniaui'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
                    xmlns:local='using:Avalonia.Base.UnitTests.Data.Core'
                    x:DataType='local:NullConditionalBindingTests+First'
                    x:CompileBindings='{{{compileBindings}}}'>
                <local:ErrorCollectingTextBox Text='{Binding Second.Third?.Final, TargetNullValue=ItsNull}'/>
            </Window>
            """;
        var data = new First(new Second(null));
        var window = CreateTarget(xaml, data);
        var textBox = Assert.IsType<ErrorCollectingTextBox>(window.Content);

        Assert.Equal("ItsNull", textBox.Text);
        Assert.Null(textBox.Error);
        Assert.Equal(BindingValueType.Value, textBox.ErrorState);
        Assert.Empty(log.Messages);
    }

    private Window CreateTarget(string xaml, object? data)
    {
        var result = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        result.DataContext = data;
        result.Show();
        return result;
    }

    private static IDisposable Start()
    {
        return UnitTestApplication.Start(TestServices.StyledWindow);
    }

    public record First(Second? Second);
    public record Second(Third? Third);
    public record Third(string Final)
    {
        public string Greeting() => "Hello!";
    }

    private class TestLogger : ILogSink, IDisposable
    {
        private TestLogger() { }

        public IList<string> Messages { get; } = [];

        public static TestLogger Create()
        {
            var result = new TestLogger();
            Logger.Sink = result;
            return result;
        }

        public void Dispose() => Logger.Sink = null;

        public bool IsEnabled(LogEventLevel level, string area)
        {
            return level >= LogEventLevel.Warning && area == LogArea.Binding;
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
        {
            Messages.Add(messageTemplate);
        }

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            Messages.Add(messageTemplate);
        }
    }
}
