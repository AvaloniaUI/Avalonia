using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentMenuItemTests() : MenuItemTests(typeof(FluentTheme));
public class SimpleMenuItemTests() : MenuItemTests(typeof(SimpleTheme));

public abstract class MenuItemTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Fact]
    public void Icon_And_Header_Should_Be_Vertically_Centered()
    {
        CreateAttachedTheme();
        var target = new MenuItem
        {
            Header = "Item",
            Icon = new Border { Width = 16, Height = 16 },
        };
        var window = new Window { Content = new StackPanel { Children = { target } } };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var parts = target.GetVisualDescendants().OfType<Control>().ToList();
        AssertVerticallyCentered(target, parts.Single(x => x.Name == "PART_IconPresenter"));
        AssertVerticallyCentered(target, parts.Single(x => x.Name == "PART_HeaderPresenter"));
        window.Close();
    }

    private static void AssertVerticallyCentered(MenuItem item, Control part)
    {
        var center = part.TranslatePoint(default, item)!.Value.Y + part.Bounds.Height / 2;

        // Layout rounding can move the part by up to half a pixel.
        Assert.Equal(item.Bounds.Height / 2, center, 0.5);
    }
}
