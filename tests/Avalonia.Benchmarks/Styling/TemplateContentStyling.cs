using System;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling;

[MemoryDiagnoser]
public class TemplateContentStyling
{
    private IDisposable _app;
    private Window _window;
    private Window _expanderWindow;
    private Window _splitButtonWindow;
    private Expander _expander;
    private SplitButton _splitButton;

    [Params(false, true)]
    public bool Fluent { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var services = Fluent
            ? TestServices.StyledWindow.With(theme: () => new FluentTheme(), globalClock: new MockGlobalClock())
            : TestServices.StyledWindow;
        _app = UnitTestApplication.Start(services);
        _window = CreateWindow();
        _expanderWindow = CreateWindow();
        _splitButtonWindow = CreateWindow();
        _expanderWindow.Content = _expander = CreateExpander();
        _splitButtonWindow.Content = _splitButton = CreateSplitButton();
        _window.Show();
        _expanderWindow.Show();
        _splitButtonWindow.Show();
    }

    [Benchmark]
    public void Add_And_Style_Expander()
    {
        _window.Content = CreateExpander();
        _window.UpdateLayout();
    }

    [Benchmark]
    public void Add_And_Style_SplitButton()
    {
        _window.Content = CreateSplitButton();
        _window.UpdateLayout();
    }

    [Benchmark]
    public void Replace_Expander_Header()
    {
        _expander.Header = new TextBlock { Text = "User header" };
        _expanderWindow.UpdateLayout();
    }

    [Benchmark]
    public void Replace_SplitButton_Content()
    {
        _splitButton.Content = new TextBlock { Text = "User content" };
        _splitButtonWindow.UpdateLayout();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _window.Close();
        _expanderWindow.Close();
        _splitButtonWindow.Close();
        _app.Dispose();
    }

    private static Expander CreateExpander() => new()
    {
        Header = new TextBlock { Text = "User header" },
        Content = new TextBlock { Text = "User content" },
        IsExpanded = true,
        ContentTransition = null,
    };

    private static SplitButton CreateSplitButton() => new()
    {
        Content = new TextBlock { Text = "User content" },
    };

    private static Window CreateWindow()
    {
        var window = new Window
        {
            Width = 400,
            Height = 300,
            Styles =
            {
                new Style(x => x.OfType<Expander>().Template().OfType<TextBlock>())
                {
                    Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Red) },
                },
                new Style(x => x.OfType<SplitButton>().Template().OfType<TextBlock>())
                {
                    Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Red) },
                },
            },
        };

        // Make repeated styling passes visible in both execution time and allocations.
        for (var i = 0; i < 100; ++i)
        {
            window.Styles.Add(new Style(x => x.Is<Control>().Class("foo").Class("bar").Class("baz"))
            {
                Setters = { new Setter(Control.TagProperty, "Styled") },
            });
        }

        return window;
    }
}
