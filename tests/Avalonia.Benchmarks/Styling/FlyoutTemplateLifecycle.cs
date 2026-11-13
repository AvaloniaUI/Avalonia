using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling;

[MemoryDiagnoser]
public class FlyoutTemplateLifecycle
{
    private IDisposable _app;
    private Window _window;
    private DropDownButton _button;
    private Flyout _flyout;

    [GlobalSetup]
    public void Setup()
    {
        _app = UnitTestApplication.Start(TestServices.StyledWindow.With(
            windowingPlatform: new MockWindowingPlatform(null,
                x => MockWindowingPlatform.CreatePopupMock(x).Object)));
        _flyout = new Flyout { Content = CreateContent() };
        _button = new DropDownButton { Flyout = _flyout };
        var host = new ContentControl
        {
            Tag = 24d,
            Template = new FuncControlTemplate<ContentControl>((_, _) => _button),
        };
        _window = new Window
        {
            Width = 400,
            Height = 300,
            Content = host,
            Styles =
            {
                new Style(x => x.OfType<ContentControl>().Template().OfType<PathIcon>())
                {
                    Setters = { new Setter(PathIcon.ForegroundProperty, Brushes.Red) },
                },
            },
        };
        _window.Show();
        host.ApplyTemplate();
        // Both benchmarks reuse an already materialized FlyoutPresenter.
        _flyout.ShowAt(_button);
        _flyout.Hide();
    }

    [Benchmark]
    public void Reopen_Unchanged_Content()
    {
        _flyout.ShowAt(_button);
        _flyout.Hide();
    }

    [Benchmark]
    public void Replace_Content_And_Reopen()
    {
        _flyout.Content = CreateContent();
        _flyout.ShowAt(_button);
        _flyout.Hide();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _flyout.Hide();
        _window.Close();
        _app.Dispose();
    }

    private static PathIcon CreateContent() => new()
    {
        [!Control.WidthProperty] = new TemplateBinding(Control.TagProperty),
    };
}
