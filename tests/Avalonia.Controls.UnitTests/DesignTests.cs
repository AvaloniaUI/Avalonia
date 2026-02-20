using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.Styling;
using System;
using System.Linq;
using Avalonia.Controls.Primitives;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class DesignTests : ScopedTestBase
{
    [Fact]
    public void Should_Preview_Resource_Dictionary_With_Template()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var dictionary = new ResourceDictionary { ["TestColor"] = Colors.Green };
        Design.SetPreviewWith(dictionary,
            new FuncTemplate<Control>(static () =>
                new Border { [!Border.BackgroundProperty] = new DynamicResourceExtension("TestColor") }));

        var preview = Design.CreatePreviewWithControl(dictionary);

        var border = Assert.IsType<Border>(preview);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)border.Background!).Color);
    }

    [Fact]
    public void Should_Preview_DataTemplate_With_ContentControl()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        const string testData = "Test Data";
        var dataTemplate = new FuncDataTemplate<string>((data, _) =>
            new TextBlock { Text = data });
        Design.SetPreviewWith(dataTemplate,
            new FuncTemplate<Control>(static () => new ContentControl { Content = testData }));

        var preview = Design.CreatePreviewWithControl(dataTemplate);

        var previewContentControl = Assert.IsType<ContentControl>(preview);
        Assert.Equal(testData, previewContentControl.Content);
        Assert.Same(dataTemplate, previewContentControl.ContentTemplate);
    }

    [Fact]
    public void Should_Preview_DataTemplate_With_DataContext()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        const string testData = "Test Data";
        var dataTemplate = new FuncDataTemplate<string>((data, _) =>
            new TextBlock { Text = data });
        Design.SetDataContext(dataTemplate, testData);

        var preview = Design.CreatePreviewWithControl(dataTemplate);

        var previewContentControl = Assert.IsType<ContentControl>(preview);
        Assert.Equal(testData, previewContentControl.Content);
        Assert.Same(dataTemplate, previewContentControl.ContentTemplate);
    }

    [Fact]
    public void Should_Preview_Control_With_Another_Control()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var control = new TextBlock();
        Design.SetPreviewWith(control,
            new FuncTemplate<Control>(static () => new Border()));

        var preview = Design.CreatePreviewWithControl(control);

        Assert.IsType<Border>(preview);
    }

    [Fact]
    public void Should_Apply_Design_Mode_Properties()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var control = new ContentControl();

        Design.SetWidth(control, 200);
        Design.SetHeight(control, 150);
        Design.SetDataContext(control, "TestDataContext");
        Design.SetDesignStyle(control,
            new Style(x => x.OfType<ContentControl>())
            {
                Setters = { new Setter(TemplatedControl.BackgroundProperty, Brushes.Yellow) }
            });

        Design.ApplyDesignModeProperties(control, control);

        Assert.Equal(200, control.Width);
        Assert.Equal(150, control.Height);
        Assert.Equal("TestDataContext", control.DataContext);
        Assert.Contains(control.Styles,
            s => ((Style)s).Setters.OfType<Setter>().First().Property == TemplatedControl.BackgroundProperty);
    }

    [Fact]
    public void Should_Not_Throw_Exception_On_Generic_Style()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var preview = Design.CreatePreviewWithControl(new Style(x => x.OfType<Button>()));

        // We are not going to test specific content of the placeholder preview control.
        // But it should not throw and should not return null at least.
        Assert.NotNull(preview);
    }

    [Fact]
    public void Should_Not_Throw_Exception_On_Generic_Resource_Dictionary()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var preview = Design.CreatePreviewWithControl(new ResourceDictionary());

        Assert.NotNull(preview);
    }

    [Fact]
    public void Should_Not_Throw_Exception_On_Generic_Data_Template()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var preview = Design.CreatePreviewWithControl(new FuncDataTemplate<string>((data, _) =>
            new TextBlock { Text = data }));

        Assert.NotNull(preview);
    }

    [Fact]
    public void Should_Not_Throw_Exception_On_Application()
    {
        using var _ = UnitTestApplication.Start(TestServices.StyledWindow);

        var app = new Application();
        var preview = Design.CreatePreviewWithControl(app);

        Assert.NotNull(preview);
    }
}
