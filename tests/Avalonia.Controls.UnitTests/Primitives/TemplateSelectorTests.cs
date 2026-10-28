using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives;

public class TemplateSelectorTests : ScopedTestBase
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Template_Selector_Should_Not_Style_Expander_Header(bool fluent, bool replaceHeader)
    {
        using var app = UnitTestApplication.Start(CreateServices(fluent));
        var header = new TextBlock { Text = "User header" };
        var target = new Expander { Header = header };
        var window = new Window
        {
            Styles = { CreateTemplateStyle<Expander>() },
            Content = target,
        };

        window.Show();

        if (replaceHeader)
        {
            header = new TextBlock { Text = "Replacement user header" };
            target.Header = header;
            window.LayoutManager.ExecuteLayoutPass();
        }

        Assert.Contains(header, target.GetVisualDescendants());
        Assert.NotEqual(Brushes.Red, header.Foreground);
        Assert.Null(header.TemplatedParent);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void Template_Selector_Should_Not_Style_SplitButton_Content(bool fluent, bool replaceContent)
    {
        using var app = UnitTestApplication.Start(CreateServices(fluent));
        var content = new TextBlock { Text = "User content" };
        var target = new SplitButton { Content = content };
        var window = new Window
        {
            Styles = { CreateTemplateStyle<SplitButton>() },
            Content = target,
        };

        window.Show();

        if (replaceContent)
        {
            content = new TextBlock { Text = "Replacement user content" };
            target.Content = content;
            window.LayoutManager.ExecuteLayoutPass();
        }

        Assert.Contains(content, target.GetVisualDescendants());
        Assert.NotEqual(Brushes.Red, content.Foreground);
        Assert.Null(content.TemplatedParent);
    }

    [Fact]
    public void Template_Selector_Should_Style_TextBlock_Created_In_Template()
    {
        var text = new TextBlock();
        var target = new Expander
        {
            Template = new FuncControlTemplate<Expander>((_, _) => new Button { Content = text }),
        };
        var root = new TestRoot
        {
            Styles = { CreateTemplateStyle<Expander>() },
            Child = target,
        };

        target.ApplyTemplate();

        Assert.Equal(Brushes.Red, text.Foreground);
        Assert.Same(target, text.TemplatedParent);
    }

    [Fact]
    public void Setter_Template_Children_Should_Resolve_TemplateBindings_Without_Control_Template()
    {
        var text = new TextBlock
        {
            [!TextBlock.TextProperty] = new TemplateBinding(ContentControl.TagProperty),
        };
        var target = new ContentControl { Tag = "Template value" };
        var root = new TestRoot
        {
            Styles =
            {
                new Style(x => x.OfType<ContentControl>())
                {
                    Setters =
                    {
                        new Setter(ContentControl.ContentProperty,
                            new FuncTemplate<Control>(() => new Border { Child = text })),
                    },
                },
            },
            Child = target,
        };

        Assert.Equal("Template value", text.Text);
        Assert.Same(target, text.TemplatedParent);
        Assert.Same(target, ((Border)target.Content!).TemplatedParent);
    }

    private static Style CreateTemplateStyle<T>() where T : Control =>
        new(x => x.OfType<T>().Template().OfType<TextBlock>())
        {
            Setters = { new Setter(TextBlock.ForegroundProperty, Brushes.Red) },
        };

    private static TestServices CreateServices(bool fluent) => fluent
        ? TestServices.StyledWindow.With(theme: () => new FluentTheme(), globalClock: new MockGlobalClock())
        : TestServices.StyledWindow;
}
