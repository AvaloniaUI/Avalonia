using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class AutoCompleteBoxAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Creates_AutoCompleteBoxAutomationPeer()
    {
        var target = new AutoCompleteBox();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsType<AutoCompleteBoxAutomationPeer>(peer);
    }

    [Fact]
    public void Implements_IExpandCollapse_And_IValue_Providers()
    {
        var target = new AutoCompleteBox();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        Assert.IsAssignableFrom<IValueProvider>(peer);
        Assert.False(peer is IInvokeProvider);
    }

    [Fact]
    public void ControlType_Is_Group_And_ClassName_Is_AutoCompleteBox()
    {
        var target = new AutoCompleteBox();
        var peer = (AutoCompleteBoxAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal(nameof(AutoCompleteBox), peer.GetClassName());
    }

    [Fact]
    public void ExpandCollapse_Tracks_IsDropDownOpen()
    {
        var target = new AutoCompleteBox
        {
            ItemsSource = new[] { "alpha" },
            Text = "a"
        };
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);
        Assert.True(peer.ShowsMenu);

        target.IsDropDownOpen = false;
        Assert.False(target.IsDropDownOpen);

        peer.Expand();
        Assert.True(target.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

        peer.Collapse();
        Assert.False(target.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
    }

    [Fact]
    public void Value_Tracks_And_Sets_Text()
    {
        var target = new AutoCompleteBox { Text = "one" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("one", peer.Value);
        peer.SetValue("two");
        Assert.Equal("two", target.Text);
        Assert.Equal("two", ((IValueProvider)peer).Value);
    }

    [Fact]
    public void ValueProvider_IsMutable()
    {
        var target = new AutoCompleteBox();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.False(peer.IsReadOnly);
    }

    [Fact]
    public void Property_Change_Events_Raise_For_DropDown_And_Text()
    {
        var target = new AutoCompleteBox();
        var peer = (AutoCompleteBoxAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPropertyChangedEventArgs? expandCollapseChanged = null;
        AutomationPropertyChangedEventArgs? valueChanged = null;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
            {
                expandCollapseChanged = e;
            }
            else if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                valueChanged = e;
            }
        };

        target.IsDropDownOpen = true;
        Assert.NotNull(expandCollapseChanged);
        Assert.Equal(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, expandCollapseChanged!.Property);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapseChanged.OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapseChanged.NewValue);

        target.Text = "query";
        Assert.NotNull(valueChanged);
        Assert.Equal(ValuePatternIdentifiers.ValueProperty, valueChanged!.Property);
        Assert.Equal(string.Empty, valueChanged.OldValue);
        Assert.Equal("query", valueChanged.NewValue);
    }
}

public class CalendarAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Creates_CalendarAutomationPeer()
    {
        var target = new Calendar();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsType<CalendarAutomationPeer>(peer);
    }

    [Fact]
    public void Implements_ISelection_And_IValue_Providers()
    {
        var target = new Calendar();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<ISelectionProvider>(peer);
        Assert.IsAssignableFrom<IValueProvider>(peer);
    }

    [Fact]
    public void ControlType_Is_Calendar_And_ClassName_Is_Calendar()
    {
        var target = new Calendar();
        var peer = (CalendarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(AutomationControlType.Calendar, peer.GetAutomationControlType());
        Assert.Equal(nameof(Calendar), peer.GetClassName());
    }

    [Theory]
    [InlineData(CalendarSelectionMode.SingleDate, false)]
    [InlineData(CalendarSelectionMode.SingleRange, true)]
    [InlineData(CalendarSelectionMode.MultipleRange, true)]
    [InlineData(CalendarSelectionMode.None, false)]
    public void CanSelectMultiple_Reflects_SelectionMode(CalendarSelectionMode selectionMode, bool canSelectMultiple)
    {
        var target = new Calendar { SelectionMode = selectionMode };
        var peer = (ISelectionProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(canSelectMultiple, peer.CanSelectMultiple);
        Assert.False(peer.IsSelectionRequired);
    }

    [Fact]
    public void Selection_Events_Include_Selection_And_Value_Properties()
    {
        var target = new Calendar { SelectionMode = CalendarSelectionMode.SingleDate };
        var peer = (CalendarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPropertyChangedEventArgs? selectionChanged = null;
        AutomationPropertyChangedEventArgs? valueChanged = null;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == SelectionPatternIdentifiers.SelectionProperty)
            {
                selectionChanged = e;
            }
            else if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                valueChanged = e;
            }
        };

        target.SelectedDate = new DateTime(2010, 1, 1);

        Assert.NotNull(selectionChanged);
        Assert.NotNull(valueChanged);
        Assert.Equal(SelectionPatternIdentifiers.SelectionProperty, selectionChanged!.Property);
        Assert.Equal(ValuePatternIdentifiers.ValueProperty, valueChanged!.Property);
    }

    [Fact]
    public void Value_Joins_Selected_Dates_With_CurrentCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");

            var selectedDates = new[] { new DateTime(2026, 5, 7), new DateTime(2026, 5, 8) };
            var target = new Calendar
            {
                SelectionMode = CalendarSelectionMode.MultipleRange
            };
            var peer = (CalendarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

            foreach (var date in selectedDates)
            {
                target.SelectedDates.Add(date);
            }

            Assert.Equal(
                string.Join(CultureInfo.CurrentCulture.TextInfo.ListSeparator, selectedDates.Select(x => x.ToString(CultureInfo.CurrentCulture))),
                peer.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public void SetValue_Throws_NotSupported()
    {
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(new Calendar());

        Assert.Throws<NotSupportedException>(() => peer.SetValue("2026-01-01"));
    }

    [Fact]
    public void Value_Is_ReadOnly()
    {
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(new Calendar());

        Assert.True(peer.IsReadOnly);
    }

    [Fact]
    public void GetSelection_Returns_Empty_When_DayButton_Not_Realized()
    {
        var target = new Calendar { SelectionMode = CalendarSelectionMode.SingleDate, SelectedDate = new DateTime(2026, 5, 7) };
        var peer = (CalendarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        var selection = peer.GetSelection();

        Assert.Empty(selection);
    }

    [Fact]
    public void GetSelection_Returns_Realized_DayButton_Peers()
    {
        var selectedDate = new DateTime(2026, 5, 7);
        var target = new Calendar
        {
            SelectionMode = CalendarSelectionMode.SingleDate,
            DisplayDate = new DateTime(2026, 5, 1),
            SelectedDate = selectedDate,
        };
        var monthView = new Grid();
        var calendarItem = new CalendarItem
        {
            Owner = target,
            MonthView = monthView
        };
        target.Root = new Panel { Children = { calendarItem } };

        for (var i = 0; i < Calendar.ColumnsPerMonth; i++)
        {
            monthView.Children.Add(new TextBlock());
        }

        for (var i = 0; i < Calendar.RowsPerMonth * Calendar.ColumnsPerMonth - Calendar.ColumnsPerMonth; i++)
        {
            monthView.Children.Add(new CalendarDayButton
            {
                Owner = target,
                DataContext = i == 0 ? selectedDate : selectedDate.AddDays(i + 1)
            });
        }

        var peer = (CalendarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);
        var selection = peer.GetSelection();

        Assert.Single(selection);
    }
}
