using System;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Styling;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Styling;

[MemoryDiagnoser]
public class SetterTemplateLifecycle
{
    private IDisposable _app;
    private TestRoot _root;
    private ContentControl _target;
    private IControlTemplate _firstTemplate;
    private IControlTemplate _secondTemplate;

    [Params(0, 1, 16, 64)]
    public int Descendants { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _app = UnitTestApplication.Start(TestServices.StyledWindow);
        _firstTemplate = CreateControlTemplate();
        _secondTemplate = CreateControlTemplate();
        _root = new TestRoot
        {
            Styles =
            {
                new Style(x => x.OfType<ContentControl>().Class("active"))
                {
                    Setters =
                    {
                        new Setter(ContentControl.ContentProperty, new FuncTemplate<Control>(CreateContent)),
                    },
                },
            },
            Child = _target = CreateTarget(),
        };
    }

    [Benchmark]
    public void Materialize_Setter_Content()
    {
        // Replacing the target also tears down the previous setter instance.
        _root.Child = CreateTarget();
    }

    [Benchmark]
    public void Deactivate_And_Reactivate_Setter()
    {
        _target.Classes.Remove("active");
        _target.Classes.Add("active");
    }

    [Benchmark]
    public void Materialize_And_Replace_Control_Template()
    {
        // Start fresh each operation: a broken implementation can lose ownership on
        // the first replacement, making subsequent replacements artificially cheap.
        var target = CreateTarget();
        _root.Child = target;
        target.Template = _firstTemplate;
        target.ApplyTemplate();
        target.Presenter!.UpdateChild();
        target.Template = _secondTemplate;
        target.ApplyTemplate();
        target.Presenter!.UpdateChild();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _root.Child = null;
        _root.Styles.Clear();
        _app.Dispose();
    }

    private Control CreateContent()
    {
        var panel = new StackPanel();
        for (var i = 0; i < Descendants; ++i)
        {
            panel.Children.Add(new TextBlock
            {
                [!TextBlock.TextProperty] = new TemplateBinding(Control.TagProperty),
            });
        }
        return panel;
    }

    private static ContentControl CreateTarget() => new()
    {
        Classes = { "active" },
        Tag = "Template binding value",
    };

    private static IControlTemplate CreateControlTemplate() =>
        new FuncControlTemplate<ContentControl>((_, scope) => new ContentPresenter
        {
            Name = "PART_ContentPresenter",
            [!ContentPresenter.ContentProperty] = new TemplateBinding(ContentControl.ContentProperty),
        }.RegisterInNameScope(scope));
}
