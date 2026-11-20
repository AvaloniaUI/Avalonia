using System;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Fluent2;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Themes.UnitTests;

[Trait("Category", "FluentTheme")]
public class FluentLabelTests() : LabelTests(typeof(FluentTheme));
[Trait("Category", "Fluent2Theme")]
public class Fluent2LabelTests() : LabelTests(typeof(Fluent2Theme));
[Trait("Category", "SimpleTheme")]
public class SimpleLabelTests() : LabelTests(typeof(SimpleTheme));

public abstract class LabelTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Clicking_Label_Padding_Should_Focus_Target()
    {
        CreateAttachedTheme();
        var textBox = new TextBox();
        var label = new Label { Content = "Name", Padding = new Thickness(20), Target = textBox };
        var window = new Window { Content = new StackPanel { Children = { label, textBox } } };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        // Inside the padding, outside of the text.
        var point = label.TranslatePoint(new Point(5, 5), window)!.Value;
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.True(textBox.IsFocused);
    }
}
