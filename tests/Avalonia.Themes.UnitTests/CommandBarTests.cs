using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Simple;
using Avalonia.Themes.UnitTests.Utilities;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Themes.UnitTests;

public class FluentCommandBarTests() : CommandBarTests(typeof(FluentTheme));
public class SimpleCommandBarTests() : CommandBarTests(typeof(SimpleTheme));

public abstract class CommandBarTests(Type typeEntryPoint) : ThemeTestBase(typeEntryPoint)
{
    [Theory]
    [InlineData(2)]
    [InlineData(20)]
    public void Overflow_Items_Should_Remain_Accessible_When_Height_Is_Constrained(int itemCount)
    {
        CreateAttachedTheme();
        var commandBar = new CommandBar { VerticalAlignment = VerticalAlignment.Top };
        for (var i = 0; i < itemCount; ++i)
            commandBar.SecondaryCommands.Add(new CommandBarButton { Label = $"Command {i}", Height = 32 });

        var window = new Window { Width = 400, Height = 300, Content = commandBar };
        window.Show();
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var popup = commandBar.GetVisualDescendants().OfType<Popup>().Single();
        popup.MaxHeight = 200;
        commandBar.IsOpen = true;
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var popupChild = popup.Child!;
        var presenter = popupChild.GetVisualDescendants().OfType<ItemsControl>().Single();
        var last = (Control)commandBar.SecondaryCommands[^1];
        presenter.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.End });
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        Assert.True(last.IsFocused);
        var bounds = last.TranslatePoint(default, popupChild)!.Value;
        Assert.True(bounds.Y >= 0);
        Assert.True(bounds.Y + last.Bounds.Height <= popupChild.Bounds.Height);

        presenter.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Home });
        Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

        var first = (Control)commandBar.SecondaryCommands[0];
        Assert.True(first.IsFocused);
        Assert.True(first.TranslatePoint(default, popupChild)!.Value.Y >= 0);
        window.Close();
    }
}
