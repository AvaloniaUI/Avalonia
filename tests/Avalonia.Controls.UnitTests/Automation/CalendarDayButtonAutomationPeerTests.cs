using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class CalendarDayButtonAutomationPeerTests : ScopedTestBase
{
    private static readonly DateTime Date1 = new(2026, 6, 5);
    private static readonly DateTime Date2 = new(2026, 6, 10);

    private static (Calendar, CalendarDayButton, ISelectionItemProvider) CreateTarget(
        CalendarSelectionMode mode, DateTime? date = null)
    {
        var calendar = new Calendar { SelectionMode = mode };
        var dayButton = new CalendarDayButton { Owner = calendar, DataContext = date ?? Date1 };
        var peer = ControlAutomationPeer.CreatePeerForElement(dayButton);
        return (calendar, dayButton, Assert.IsAssignableFrom<ISelectionItemProvider>(peer));
    }

    [Fact]
    public void Creates_CalendarDayButtonAutomationPeer()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new CalendarDayButton());

        Assert.IsType<CalendarDayButtonAutomationPeer>(peer);
        Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
        Assert.IsAssignableFrom<IInvokeProvider>(peer);
    }

    [Fact]
    public void IsSelected_Reflects_Owner_State()
    {
        var (_, dayButton, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

        Assert.False(provider.IsSelected);
        dayButton.IsSelected = true;
        Assert.True(provider.IsSelected);
    }

    [Fact]
    public void SelectionContainer_Is_Calendar_Peer()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

        var container = provider.SelectionContainer;

        Assert.NotNull(container);
        Assert.Same(ControlAutomationPeer.CreatePeerForElement(calendar), container);
    }

    [Fact]
    public void Select_Sets_SelectedDate()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);

        provider.Select();

        Assert.Equal(Date1, calendar.SelectedDate);
    }

    [Fact]
    public void Select_Replaces_Existing_Selection_In_SingleDate_Mode()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
        calendar.SelectedDate = Date2;

        provider.Select();

        Assert.Equal(Date1, calendar.SelectedDate);
        Assert.Equal(1, calendar.SelectedDates.Count);
    }

    [Fact]
    public void Select_Is_NoOp_When_SelectionMode_None()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

        provider.Select();

        Assert.Null(calendar.SelectedDate);
        Assert.Empty(calendar.SelectedDates);
    }

    [Fact]
    public void Select_Is_NoOp_For_Blackout_Day()
    {
        var (calendar, dayButton, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
        dayButton.IsBlackout = true;

        provider.Select();

        Assert.Null(calendar.SelectedDate);
    }

    [Fact]
    public void AddToSelection_Replaces_Selection_In_SingleDate_Mode()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.SingleDate);
        calendar.SelectedDate = Date2;

        provider.AddToSelection();

        Assert.Equal(Date1, calendar.SelectedDate);
        Assert.Equal(1, calendar.SelectedDates.Count);
    }

    [Fact]
    public void AddToSelection_Appends_In_MultipleRange_Mode()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
        calendar.SelectedDates.Add(Date2);

        provider.AddToSelection();

        Assert.Equal(2, calendar.SelectedDates.Count);
        Assert.Contains(Date1, calendar.SelectedDates);
        Assert.Contains(Date2, calendar.SelectedDates);
    }

    [Fact]
    public void AddToSelection_Is_NoOp_When_SelectionMode_None()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

        provider.AddToSelection();

        Assert.Empty(calendar.SelectedDates);
    }

    [Fact]
    public void AddToSelection_Is_NoOp_For_Blackout_Day()
    {
        var (calendar, dayButton, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
        dayButton.IsBlackout = true;

        provider.AddToSelection();

        Assert.Empty(calendar.SelectedDates);
    }

    [Fact]
    public void RemoveFromSelection_Removes_Date()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.MultipleRange);
        calendar.SelectedDates.Add(Date1);
        calendar.SelectedDates.Add(Date2);

        provider.RemoveFromSelection();

        Assert.Equal(Date2, Assert.Single(calendar.SelectedDates));
    }

    [Fact]
    public void RemoveFromSelection_Is_NoOp_When_SelectionMode_None()
    {
        var (calendar, _, provider) = CreateTarget(CalendarSelectionMode.None);

        provider.RemoveFromSelection();

        Assert.Empty(calendar.SelectedDates);
    }
}
