using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentButtonSpinnerTests() : ThemeTestBase(typeof(FluentTheme))
{
    [Fact]
    public void Outer_Spin_Button_Follows_The_Right_Corners()
    {
        var (increase, decrease) = ShowNumericUpDown(Location.Right);

        AssertCorners(new CornerRadius(0), increase);
        AssertCorners(new CornerRadius(0, 12, 12, 0), decrease);
    }

    [Fact]
    public void Outer_Spin_Button_Follows_The_Left_Corners()
    {
        var (increase, decrease) = ShowNumericUpDown(Location.Left);

        AssertCorners(new CornerRadius(12, 0, 0, 12), increase);
        AssertCorners(new CornerRadius(0), decrease);
    }

    private (RepeatButton Increase, RepeatButton Decrease) ShowNumericUpDown(Location location)
    {
        CreateAttachedTheme();
        var target = new NumericUpDown
        {
            Width = 180,
            Minimum = 0,
            Value = 0,
            CornerRadius = new CornerRadius(12),
            ButtonSpinnerLocation = location,
        };
        var window = new Window { Content = target };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var buttons = target.GetVisualDescendants().OfType<RepeatButton>().ToList();
        return (buttons.Single(x => x.Name == "PART_IncreaseButton"),
            buttons.Single(x => x.Name == "PART_DecreaseButton"));
    }

    private static void AssertCorners(CornerRadius expected, RepeatButton button)
    {
        Assert.Equal(expected, button.CornerRadius);

        // The template's presenter paints the button background
        var presenter = button.GetVisualChildren().OfType<ContentPresenter>().Single();
        Assert.Equal(expected, presenter.CornerRadius);
    }
}
