using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Headless.UnitTests;

public class DateTimePickerTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void DatePicker_Focuses_A_Selector_When_Opened()
    {
        var picker = new DatePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        AssertHelper.True(Selectors(picker).Any(selector => selector.IsFocused),
            "no selector in the popup took focus");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void TimePicker_Focuses_A_Selector_When_Opened()
    {
        var picker = new TimePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        AssertHelper.True(Selectors(picker).Any(selector => selector.IsFocused),
            "no selector in the popup took focus");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void DatePicker_Tab_Follows_The_Columns_On_Screen()
    {
        var picker = new DatePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        var selectors = Selectors(picker).Where(selector => selector.IsEffectivelyVisible).ToArray();
        var content = (Visual)picker.GetVisualDescendants().OfType<Popup>().First().Child!;
        var leftToRight = selectors
            .OrderBy(selector => selector.TranslatePoint(new Point(), content)?.X ?? 0)
            .ToArray();

        var visited = new List<DateTimePickerPanel> { Focused(selectors) };

        for (var i = 1; i < leftToRight.Length; i++)
        {
            Tab(selectors, KeyModifiers.None);
            visited.Add(Focused(selectors));
        }

        AssertHelper.True(visited.SequenceEqual(leftToRight),
            "tab did not follow the columns from left to right");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void DatePicker_Shift_Tab_Goes_Back()
    {
        var picker = new DatePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        var selectors = Selectors(picker).Where(selector => selector.IsEffectivelyVisible).ToArray();
        var first = Focused(selectors);

        Tab(selectors, KeyModifiers.None);
        AssertHelper.False(ReferenceEquals(first, Focused(selectors)), "tab did not move the focus");

        Tab(selectors, KeyModifiers.Shift);
        AssertHelper.True(ReferenceEquals(first, Focused(selectors)),
            "shift and tab did not come back to where it started");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void TimePicker_Tab_Reaches_The_Buttons_After_The_Columns()
    {
        var picker = new TimePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        var selectors = Selectors(picker).Where(selector => selector.IsEffectivelyVisible).ToArray();
        var content = picker.GetVisualDescendants().OfType<Popup>().First().Child!;
        var leftToRight = selectors
            .OrderBy(selector => selector.TranslatePoint(new Point(), content)?.X ?? 0)
            .ToArray();

        var visited = new List<DateTimePickerPanel> { Focused(selectors) };

        for (var i = 1; i < leftToRight.Length; i++)
        {
            Tab(selectors, KeyModifiers.None);
            visited.Add(Focused(selectors));
        }

        AssertHelper.True(visited.SequenceEqual(leftToRight),
            "tab did not follow the columns from left to right");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void TimePicker_Shift_Tab_Goes_Back()
    {
        var picker = new TimePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        Open(picker);

        var selectors = Selectors(picker).Where(selector => selector.IsEffectivelyVisible).ToArray();
        var first = Focused(selectors);

        Tab(selectors, KeyModifiers.None);
        AssertHelper.False(ReferenceEquals(first, Focused(selectors)), "tab did not move the focus");

        Tab(selectors, KeyModifiers.Shift);
        AssertHelper.True(ReferenceEquals(first, Focused(selectors)),
            "shift and tab did not come back to where it started");

        window.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void CalendarDatePicker_Escape_Puts_The_Date_Back()
    {
        var chosen = new System.DateTime(2026, 5, 15);
        var picker = new CalendarDatePicker { SelectedDate = chosen };
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        picker.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var popup = picker.GetVisualDescendants().OfType<Popup>().First();
        var calendar = popup.Child!.GetSelfAndVisualDescendants().OfType<Calendar>().First();

        Press(calendar, Key.Down);
        AssertHelper.False(picker.SelectedDate == chosen, "the arrow key did not move the date");

        Press(calendar, Key.Escape);

        AssertHelper.True(picker.SelectedDate == chosen, "escape did not put the date back");
        AssertHelper.False(picker.IsDropDownOpen, "escape did not close the drop-down");

        window.Close();
    }

    private static void Press(Control target, Key key)
    {
        target.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
        Dispatcher.UIThread.RunJobs();
    }

    private static DateTimePickerPanel Focused(DateTimePickerPanel[] selectors)
        => selectors.First(selector => selector.IsFocused);

    private static void Tab(DateTimePickerPanel[] selectors, KeyModifiers modifiers)
    {
        var focused = selectors.First(selector => selector.IsFocused);

        focused.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Tab,
            KeyModifiers = modifiers
        });

        Dispatcher.UIThread.RunJobs();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void CalendarDatePicker_Focuses_Its_Calendar_When_Opened()
    {
        var picker = new CalendarDatePicker();
        var window = new Window { Width = 300, Height = 200, Content = picker };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        picker.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var popup = picker.GetVisualDescendants().OfType<Popup>().First();
        var calendar = popup.Child!.GetSelfAndVisualDescendants().OfType<Calendar>().First();

        AssertHelper.True(calendar.IsFocused, "the calendar did not take focus");

        window.Close();
    }

    private static void Open(Control picker)
    {
        var button = picker.GetVisualDescendants().OfType<Button>()
            .First(b => b.Name == "PART_FlyoutButton");

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    private static DateTimePickerPanel[] Selectors(Control picker)
    {
        var popup = picker.GetVisualDescendants().OfType<Popup>().First();

        return popup.Child is { } content
            ? content.GetSelfAndVisualDescendants().OfType<DateTimePickerPanel>().ToArray()
            : [];
    }
}
