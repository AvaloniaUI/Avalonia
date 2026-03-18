using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class TabbedPageTests
{
    public class PropertyDefaults : ScopedTestBase
    {
        [Fact]
        public void TabPlacement_DefaultIsAuto()
        {
            // Auto resolves to Bottom on iOS/Android and Top everywhere else.
            var tp = new TabbedPage();
            Assert.Equal(TabPlacement.Auto, tp.TabPlacement);
        }

        [Fact]
        public void SelectedIndex_InitiallyMinusOne()
        {
            // -1 is the "no selection" sentinel used throughout the selection API.
            var tp = new TabbedPage();
            Assert.Equal(-1, tp.SelectedIndex);
        }

        [Fact]
        public void SelectedPage_InitiallyNull()
        {
            var tp = new TabbedPage();
            Assert.Null(tp.SelectedPage);
        }
    }

    public class PropertyRoundTrips : ScopedTestBase
    {
        [Theory]
        [InlineData(TabPlacement.Auto)]
        [InlineData(TabPlacement.Top)]
        [InlineData(TabPlacement.Bottom)]
        [InlineData(TabPlacement.Left)]
        [InlineData(TabPlacement.Right)]
        public void TabPlacement_RoundTrips(TabPlacement placement)
        {
            var tp = new TabbedPage { TabPlacement = placement };
            Assert.Equal(placement, tp.TabPlacement);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsKeyboardNavigationEnabled_RoundTrips(bool enabled)
        {
            var tp = new TabbedPage { IsKeyboardNavigationEnabled = enabled };
            Assert.Equal(enabled, tp.IsKeyboardNavigationEnabled);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        public void SelectedIndex_StoredBeforeTemplateApplied(int index)
        {
            var tp = new TabbedPage();
            tp.SelectedIndex = index;
            Assert.Equal(index, tp.SelectedIndex);
        }

        [Fact]
        public void PageTemplate_CanBeSetToNull()
        {
            var tp = new TabbedPage { PageTemplate = null };
            Assert.Null(tp.PageTemplate);
        }

        [Fact]
        public void PageTransition_RoundTrips()
        {
            var transition = new CrossFade(TimeSpan.FromMilliseconds(200));
            var tp = new TabbedPage { PageTransition = transition };
            Assert.Same(transition, tp.PageTransition);
        }

        [Fact]
        public void IndicatorTemplate_RoundTrips()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tp = new TabbedPage { IndicatorTemplate = template };
            Assert.Same(template, tp.IndicatorTemplate);
        }

        [Fact]
        public void IndicatorTemplate_CanBeSetToNull()
        {
            var template = new FuncDataTemplate<object>((_, _) => new Border());
            var tp = new TabbedPage { IndicatorTemplate = template };
            tp.IndicatorTemplate = null;
            Assert.Null(tp.IndicatorTemplate);
        }
    }

    public class PagesCollectionTests : ScopedTestBase
    {
        [Fact]
        public void Pages_InitiallyNonNull_EmptyList()
        {
            var tp = new TabbedPage();
            Assert.NotNull(tp.Pages);
        }

        [Fact]
        public void Pages_SetNewList_UpdatesProperty()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page> { new ContentPage { Header = "A" } };
            tp.Pages = pages;
            Assert.Same(pages, tp.Pages);
        }

        [Fact]
        public void Pages_Added_BecomeLogicalChildren()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page>();
            tp.Pages = pages;

            var page1 = new ContentPage { Header = "Tab 1" };
            var page2 = new ContentPage { Header = "Tab 2" };
            pages.Add(page1);
            pages.Add(page2);

            var children = ((ILogical)tp).LogicalChildren;
            Assert.Contains(page1, children);
            Assert.Contains(page2, children);
        }

        [Fact]
        public void Pages_Removed_RemovedFromLogicalChildren()
        {
            var tp = new TabbedPage();
            var page1 = new ContentPage { Header = "Tab 1" };
            var page2 = new ContentPage { Header = "Tab 2" };
            var pages = new AvaloniaList<Page> { page1, page2 };
            tp.Pages = pages;

            pages.Remove(page1);

            Assert.DoesNotContain(page1, ((ILogical)tp).LogicalChildren);
            Assert.Contains(page2, ((ILogical)tp).LogicalChildren);
        }

        [Fact]
        public void Pages_Replaced_OldLogicalChildrenClearedNewAdded()
        {
            var tp = new TabbedPage();
            var old = new ContentPage { Header = "Old" };
            tp.Pages = new AvaloniaList<Page> { old };

            var fresh = new ContentPage { Header = "Fresh" };
            tp.Pages = new AvaloniaList<Page> { fresh };

            Assert.DoesNotContain(old, ((ILogical)tp).LogicalChildren);
            Assert.Contains(fresh, ((ILogical)tp).LogicalChildren);
        }

        [Fact]
        public void Pages_SetNull_ClearsLogicalChildren()
        {
            var tp = new TabbedPage();
            var page = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page };
            tp.Pages = null;
            Assert.DoesNotContain(page, ((ILogical)tp).LogicalChildren);
        }

        [Fact]
        public void Pages_SetNull_ClearsCurrentPage()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page };
            tp.CallCommitSelection(0, page);
            Assert.NotNull(tp.CurrentPage);
            tp.Pages = null;
            Assert.Null(tp.CurrentPage);
        }

        [Fact]
        public void Pages_AddMultiple_AllBecomeLogicalChildren()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page>();
            tp.Pages = pages;

            var list = new List<ContentPage>();
            for (int i = 0; i < 5; i++)
            {
                var p = new ContentPage { Header = $"Tab {i}" };
                list.Add(p);
                pages.Add(p);
            }

            var children = ((ILogical)tp).LogicalChildren;
            foreach (var p in list)
                Assert.Contains(p, children);
        }

        [Fact]
        public void Pages_Clear_RemovesAllLogicalChildren()
        {
            var tp = new TabbedPage();
            var a = new ContentPage { Header = "A" };
            var b = new ContentPage { Header = "B" };
            var pages = new AvaloniaList<Page> { a, b };
            tp.Pages = pages;

            pages.Clear();

            Assert.DoesNotContain(a, ((ILogical)tp).LogicalChildren);
            Assert.DoesNotContain(b, ((ILogical)tp).LogicalChildren);
        }
    }

    public class PagesChangedEventTests : ScopedTestBase
    {
        [Fact]
        public void PagesChanged_FiresOnAdd()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page>();
            tp.Pages = pages;

            NotifyCollectionChangedEventArgs? received = null;
            tp.PagesChanged += (_, e) => received = e;

            pages.Add(new ContentPage());

            Assert.NotNull(received);
            Assert.Equal(NotifyCollectionChangedAction.Add, received!.Action);
        }

        [Fact]
        public void PagesChanged_FiresOnRemove()
        {
            var tp = new TabbedPage();
            var page = new ContentPage();
            var pages = new AvaloniaList<Page> { page };
            tp.Pages = pages;

            NotifyCollectionChangedEventArgs? received = null;
            tp.PagesChanged += (_, e) => received = e;

            pages.Remove(page);

            Assert.NotNull(received);
            Assert.Equal(NotifyCollectionChangedAction.Remove, received!.Action);
        }

        [Fact]
        public void PagesChanged_NotFiredAfterPagesReplaced()
        {
            var tp = new TabbedPage();
            var oldPages = new AvaloniaList<Page>();
            tp.Pages = oldPages;
            bool fired = false;
            tp.PagesChanged += (_, _) => fired = true;

            tp.Pages = new AvaloniaList<Page>();
            oldPages.Add(new ContentPage());
            Assert.False(fired);
        }

        [Fact]
        public void PagesChanged_Add_ArgsContainAddedPage()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page>();
            tp.Pages = pages;

            NotifyCollectionChangedEventArgs? received = null;
            tp.PagesChanged += (_, e) => received = e;

            var page = new ContentPage { Header = "New" };
            pages.Add(page);

            Assert.NotNull(received);
            Assert.NotNull(received!.NewItems);
            Assert.True(received.NewItems!.Contains(page));
        }

        [Fact]
        public void PagesChanged_Remove_ArgsContainRemovedPage()
        {
            var tp = new TabbedPage();
            var page = new ContentPage { Header = "ToRemove" };
            var pages = new AvaloniaList<Page> { page };
            tp.Pages = pages;

            NotifyCollectionChangedEventArgs? received = null;
            tp.PagesChanged += (_, e) => received = e;

            pages.Remove(page);

            Assert.NotNull(received);
            Assert.NotNull(received!.OldItems);
            Assert.True(received.OldItems!.Contains(page));
        }

        [Fact]
        public void PagesChanged_FiresOnClear_WithResetAction()
        {
            var tp = new TabbedPage();
            var pages = new AvaloniaList<Page>
            {
                new ContentPage { Header = "A" },
                new ContentPage { Header = "B" },
            };
            tp.Pages = pages;

            NotifyCollectionChangedEventArgs? received = null;
            tp.PagesChanged += (_, e) => received = e;

            pages.Clear();

            Assert.NotNull(received);
            Assert.Equal(NotifyCollectionChangedAction.Reset, received!.Action);
        }
    }

    public class SelectionTests : ScopedTestBase
    {
        [Fact]
        public void SelectionChanged_FiresWhenSelectionChanges()
        {
            var tp = new TestableTabbedPage();

            PageSelectionChangedEventArgs? received = null;
            tp.SelectionChanged += (_, e) => received = e;

            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);
            tp.CallCommitSelection(1, page2);

            Assert.NotNull(received);
            Assert.Same(page1, received!.PreviousPage);
            Assert.Same(page2, received!.CurrentPage);
        }

        [Fact]
        public void SelectionChanged_NotFiredWhenSamePageSelected()
        {
            var tp = new TestableTabbedPage();
            int count = 0;
            tp.SelectionChanged += (_, _) => count++;

            var page = new ContentPage { Header = "A" };
            tp.CallCommitSelection(0, page);
            int countAfterFirst = count;
            tp.CallCommitSelection(0, page);

            Assert.Equal(1, countAfterFirst); // first commit must fire exactly once
            Assert.Equal(1, count);           // second commit (same page) must not fire again
        }

        [Fact]
        public void CommitSelection_UpdatesCurrentPage()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "X" };
            tp.CallCommitSelection(0, page);

            Assert.Same(page, tp.CurrentPage);
            Assert.Same(page, tp.SelectedPage);
            Assert.Equal(0, tp.SelectedIndex);
        }

        [Fact]
        public void CommitSelection_SequentialSelections_TracksCorrectPages()
        {
            var tp = new TestableTabbedPage();
            var pages = new[]
            {
                new ContentPage { Header = "Feed" },
                new ContentPage { Header = "Explore" },
                new ContentPage { Header = "Profile" },
            };

            var events = new List<(Page? prev, Page? curr)>();
            tp.SelectionChanged += (_, e) => events.Add((e.PreviousPage, e.CurrentPage));

            tp.CallCommitSelection(0, pages[0]);
            tp.CallCommitSelection(1, pages[1]);
            tp.CallCommitSelection(2, pages[2]);
            tp.CallCommitSelection(0, pages[0]);

            Assert.Equal(4, events.Count);
            Assert.Null(events[0].prev);
            Assert.Same(pages[0], events[0].curr);
            Assert.Same(pages[0], events[1].prev);
            Assert.Same(pages[1], events[1].curr);
            Assert.Same(pages[1], events[2].prev);
            Assert.Same(pages[2], events[2].curr);
            Assert.Same(pages[2], events[3].prev);
            Assert.Same(pages[0], events[3].curr);
        }

        [Fact]
        public void CommitSelection_NullPage_SetsCurrentPageToNull()
        {
            var tp = new TestableTabbedPage();
            tp.CallCommitSelection(0, new ContentPage());
            tp.CallCommitSelection(-1, null);

            Assert.Null(tp.CurrentPage);
            Assert.Null(tp.SelectedPage);
            Assert.Equal(-1, tp.SelectedIndex);
        }

        [Fact]
        public void CommitSelection_RapidChanges_TracksFinalState()
        {
            var tp = new TestableTabbedPage();
            var pages = new[]
            {
                new ContentPage { Header = "A" },
                new ContentPage { Header = "B" },
                new ContentPage { Header = "C" },
            };

            for (int i = 0; i < pages.Length; i++)
                tp.CallCommitSelection(i, pages[i]);

            Assert.Same(pages[2], tp.CurrentPage);
            Assert.Same(pages[2], tp.SelectedPage);
            Assert.Equal(2, tp.SelectedIndex);
        }

        [Fact]
        public void CurrentPageChanged_FiresOnCommitSelection()
        {
            var tp = new TestableTabbedPage();
            int count = 0;
            tp.CurrentPageChanged += (_, _) => count++;

            tp.CallCommitSelection(0, new ContentPage());

            Assert.Equal(1, count);
        }

        [Fact]
        public void CurrentPageChanged_NotFiredWhenSamePageCommitted()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage();
            tp.CallCommitSelection(0, page);

            int count = 0;
            tp.CurrentPageChanged += (_, _) => count++;

            tp.CallCommitSelection(0, page);

            Assert.Equal(0, count);
        }
    }

    public class LifecycleTests : ScopedTestBase
    {
        [Fact]
        public void CommitSelection_FiresNavigatedFrom_OnPreviousPage()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            NavigatedFromEventArgs? args = null;
            page1.NavigatedFrom += (_, e) => args = e;
            tp.CallCommitSelection(1, page2);

            Assert.NotNull(args);
            Assert.Same(page2, args!.DestinationPage);
            Assert.Equal(NavigationType.Replace, args.NavigationType);
        }

        [Fact]
        public void CommitSelection_FiresNavigatedTo_OnNewPage()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            NavigatedToEventArgs? args = null;
            page2.NavigatedTo += (_, e) => args = e;
            tp.CallCommitSelection(1, page2);

            Assert.NotNull(args);
            Assert.Same(page1, args!.PreviousPage);
            Assert.Equal(NavigationType.Replace, args.NavigationType);
        }

        [Fact]
        public void CommitSelection_LifecycleOrder_NavigatedFromNavigatedTo()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            var order = new List<string>();
            page1.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
            page2.NavigatedTo += (_, _) => order.Add("NavigatedTo");
            tp.CallCommitSelection(1, page2);

            Assert.Equal(new[] { "NavigatedFrom", "NavigatedTo" }, order);
        }

        [Fact]
        public void CommitSelection_SamePage_NoLifecycleEvents()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };
            tp.CallCommitSelection(0, page);

            var events = new List<string>();
            page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
            page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");
            tp.CallCommitSelection(0, page);

            Assert.Empty(events);
        }

        [Fact]
        public void CommitSelection_FirstPage_NavigatedToHasNullPrevious()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };

            NavigatedToEventArgs? navigatedToArgs = null;
            var events = new List<string>();
            page.NavigatedTo  += (_, e) => { navigatedToArgs = e; events.Add("NavigatedTo"); };
            page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

            tp.CallCommitSelection(0, page);

            Assert.Equal(new[] { "NavigatedTo" }, events);
            Assert.NotNull(navigatedToArgs);
            Assert.Null(navigatedToArgs!.PreviousPage);
            Assert.Equal(NavigationType.Replace, navigatedToArgs.NavigationType);
        }

        [Fact]
        public void CommitSelection_ToNull_FiresNavigatedFrom_WithNullDestination()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };
            tp.CallCommitSelection(0, page);

            NavigatedFromEventArgs? navigatedFromArgs = null;
            var events = new List<string>();
            page.NavigatedFrom += (_, e)  => { navigatedFromArgs = e; events.Add("NavigatedFrom"); };

            tp.CallCommitSelection(-1, null);

            Assert.Equal(new[] { "NavigatedFrom" }, events);
            Assert.NotNull(navigatedFromArgs);
            Assert.Null(navigatedFromArgs!.DestinationPage);
            Assert.Equal(NavigationType.Replace, navigatedFromArgs.NavigationType);
        }
    }

    public class IsTabEnabledTests : ScopedTestBase
    {
        [Fact]
        public void IsTabEnabled_DefaultIsTrue()
        {
            var page = new ContentPage();
            Assert.True(TabbedPage.GetIsTabEnabled(page));
        }

        [Fact]
        public void IsTabEnabled_SetFalse_GetFalse()
        {
            var page = new ContentPage();
            TabbedPage.SetIsTabEnabled(page, false);
            Assert.False(TabbedPage.GetIsTabEnabled(page));
        }

        [Fact]
        public void IsTabEnabled_SetTrue_GetTrue()
        {
            var page = new ContentPage();
            TabbedPage.SetIsTabEnabled(page, false);
            TabbedPage.SetIsTabEnabled(page, true);
            Assert.True(TabbedPage.GetIsTabEnabled(page));
        }
    }

    public class FindNextEnabledTabTests : ScopedTestBase
    {
        [Fact]
        public void Forward_SkipsDisabled()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1, page2 };
            TabbedPage.SetIsTabEnabled(page1, false);

            int result = tp.CallFindNextEnabledTab(1, 1);
            Assert.Equal(2, result);
        }

        [Fact]
        public void Backward_SkipsDisabled()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1, page2 };
            TabbedPage.SetIsTabEnabled(page1, false);

            int result = tp.CallFindNextEnabledTab(1, -1);
            Assert.Equal(0, result);
        }

        [Fact]
        public void NoEnabledTabAhead_ReturnsMinusOne()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1 };
            TabbedPage.SetIsTabEnabled(page1, false);

            int result = tp.CallFindNextEnabledTab(1, 1);
            Assert.Equal(-1, result);
        }

        [Fact]
        public void AllEnabled_ReturnsStartIndex()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1, page2 };

            int result = tp.CallFindNextEnabledTab(1, 1);
            Assert.Equal(1, result);
        }

        [Fact]
        public void MultipleConsecutiveDisabled_SkipsAll()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            var page3 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1, page2, page3 };
            TabbedPage.SetIsTabEnabled(page1, false);
            TabbedPage.SetIsTabEnabled(page2, false);

            int result = tp.CallFindNextEnabledTab(1, 1);
            Assert.Equal(3, result);
        }

        [Fact]
        public void AllDisabled_ReturnsMinusOne()
        {
            var tp = new TestableTabbedPage();
            var page0 = new ContentPage();
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            tp.Pages = new AvaloniaList<Page> { page0, page1, page2 };
            TabbedPage.SetIsTabEnabled(page0, false);
            TabbedPage.SetIsTabEnabled(page1, false);
            TabbedPage.SetIsTabEnabled(page2, false);

            Assert.Equal(-1, tp.CallFindNextEnabledTab(0, 1));
            Assert.Equal(-1, tp.CallFindNextEnabledTab(2, -1));
        }
    }

    public class KeyboardNavigationTests : ScopedTestBase
    {
        [Fact]
        public void IsKeyboardNavigationEnabled_Default_IsTrue()
        {
            var tp = new TabbedPage();
            Assert.True(tp.IsKeyboardNavigationEnabled);
        }

        [Fact]
        public void IsKeyboardNavigationEnabled_False_RightKey_IsNotHandled()
        {
            var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = false };
            tp.Pages = new AvaloniaList<Page> { new ContentPage(), new ContentPage() };
            tp.SelectedIndex = 0;

            bool handled = tp.SimulateKeyDownReturnsHandled(Key.Right);

            Assert.False(handled);
        }

        [Fact]
        public void IsKeyboardNavigationEnabled_False_CtrlTab_IsNotHandled()
        {
            var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = false };
            tp.Pages = new AvaloniaList<Page> { new ContentPage(), new ContentPage() };
            tp.SelectedIndex = 0;

            bool handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control);

            Assert.False(handled);
        }

        [Fact]
        public void IsKeyboardNavigationEnabled_True_NoTemplate_KeyIsNotHandled()
        {
            var tp = new TestableTabbedPage { IsKeyboardNavigationEnabled = true };
            tp.Pages = new AvaloniaList<Page> { new ContentPage(), new ContentPage() };

            bool handled = tp.SimulateKeyDownReturnsHandled(Key.Right);

            Assert.False(handled);
        }
    }

    public class KeyboardNavigationWithTemplateTests : ScopedTestBase
    {
        // Builds a TabbedPage with a real PART_TabControl wired up so OnKeyDown can navigate.
        private static TestableTabbedPage MakeTabbed(int pageCount, int selectedIndex = 0,
            TabPlacement placement = TabPlacement.Top)
        {
            var tp = new TestableTabbedPage { TabPlacement = placement };
            for (int i = 0; i < pageCount; i++)
                ((AvaloniaList<Page>)tp.Pages!).Add(new ContentPage { Header = $"Tab {i}" });

            tp.Template = new FuncControlTemplate<TabbedPage>((parent, scope) =>
                new TabControl
                {
                    Name = "PART_TabControl",
                    ItemsSource = parent.Pages,
                }.RegisterInNameScope(scope));

            _ = new TestRoot { Child = tp };
            tp.ApplyTemplate();
            tp.SelectedIndex = selectedIndex;
            return tp;
        }

        [Fact]
        public void RightKey_NavigatesToNextPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            tp.SimulateKeyDown(Key.Right);
            Assert.Equal(1, tp.SelectedIndex);
        }

        [Fact]
        public void LeftKey_NavigatesToPreviousPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 1);
            tp.SimulateKeyDown(Key.Left);
            Assert.Equal(0, tp.SelectedIndex);
        }

        [Fact]
        public void DownKey_WithVerticalPlacement_NavigatesToNextPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 0, placement: TabPlacement.Left);
            tp.SimulateKeyDown(Key.Down);
            Assert.Equal(1, tp.SelectedIndex);
        }

        [Fact]
        public void UpKey_WithVerticalPlacement_NavigatesToPreviousPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 1, placement: TabPlacement.Left);
            tp.SimulateKeyDown(Key.Up);
            Assert.Equal(0, tp.SelectedIndex);
        }

        [Fact]
        public void RightKey_AtLastPage_DoesNotNavigate()
        {
            var tp = MakeTabbed(3, selectedIndex: 2);
            tp.SimulateKeyDown(Key.Right);
            Assert.Equal(2, tp.SelectedIndex);
        }

        [Fact]
        public void LeftKey_AtFirstPage_DoesNotNavigate()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            tp.SimulateKeyDown(Key.Left);
            Assert.Equal(0, tp.SelectedIndex);
        }

        [Fact]
        public void RightKey_MarksEventHandled()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            bool handled = tp.SimulateKeyDownReturnsHandled(Key.Right);
            Assert.True(handled);
        }

        [Fact]
        public void RightKey_AtLastPage_DoesNotMarkEventHandled()
        {
            var tp = MakeTabbed(3, selectedIndex: 2);
            bool handled = tp.SimulateKeyDownReturnsHandled(Key.Right);
            Assert.False(handled);
        }

        [Fact]
        public void CtrlTab_NavigatesToNextPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            bool handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control);
            Assert.Equal(1, tp.SelectedIndex);
            Assert.True(handled);
        }

        [Fact]
        public void CtrlShiftTab_NavigatesToPreviousPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 1);
            bool handled = tp.SimulateKeyDownWithModifiersReturnsHandled(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift);
            Assert.Equal(0, tp.SelectedIndex);
            Assert.True(handled);
        }

        [Fact]
        public void RtlFlowDirection_LeftKey_NavigatesToNextPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            tp.FlowDirection = FlowDirection.RightToLeft;
            tp.SimulateKeyDown(Key.Left);
            Assert.Equal(1, tp.SelectedIndex);
        }

        [Fact]
        public void RtlFlowDirection_RightKey_NavigatesToPreviousPage()
        {
            var tp = MakeTabbed(3, selectedIndex: 1);
            tp.FlowDirection = FlowDirection.RightToLeft;
            tp.SimulateKeyDown(Key.Right);
            Assert.Equal(0, tp.SelectedIndex);
        }

        [Fact]
        public void RightKey_SkipsDisabledTab()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            TabbedPage.SetIsTabEnabled((Page)((System.Collections.IList)tp.Pages!)[1]!, false);
            tp.SimulateKeyDown(Key.Right);
            Assert.Equal(2, tp.SelectedIndex);
        }

        [Fact]
        public void RightKey_AllTabsAhead_Disabled_DoesNotNavigate()
        {
            var tp = MakeTabbed(3, selectedIndex: 0);
            TabbedPage.SetIsTabEnabled((Page)((System.Collections.IList)tp.Pages!)[1]!, false);
            TabbedPage.SetIsTabEnabled((Page)((System.Collections.IList)tp.Pages!)[2]!, false);
            tp.SimulateKeyDown(Key.Right);
            Assert.Equal(0, tp.SelectedIndex);
        }
    }

    public class SelectingMultiPageTests : ScopedTestBase
    {
        [Fact]
        public void SelectedIndex_DirectProperty_RaisesChangedEvent()
        {
            var tp = new TestableTabbedPage();
            bool raised = false;
            tp.GetObservable(SelectingMultiPage.SelectedIndexProperty)
              .Subscribe(_ => raised = true);
            tp.CallCommitSelection(0, new ContentPage());
            Assert.True(raised);
        }

        [Fact]
        public void SelectedPage_DirectProperty_RaisesChangedEvent()
        {
            var tp = new TestableTabbedPage();
            bool raised = false;
            tp.GetObservable(SelectingMultiPage.SelectedPageProperty)
              .Subscribe(_ => raised = true);
            tp.CallCommitSelection(0, new ContentPage());
            Assert.True(raised);
        }
    }

    public class IconTests : ScopedTestBase
    {
        [Fact]
        public void Geometry_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var result = TabbedPage.CreateIconContent(geometry);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void PathIcon_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var pathIcon = new PathIcon { Data = geometry };
            var result = TabbedPage.CreateIconContent(pathIcon);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void EmptyString_ReturnsNull()
        {
            var result = TabbedPage.CreateIconContent("");
            Assert.Null(result);
        }

        [Fact]
        public void NullString_ReturnsNull()
        {
            var result = TabbedPage.CreateIconContent((string?)null);
            Assert.Null(result);
        }

        [Fact]
        public void Null_ReturnsNull()
        {
            var result = TabbedPage.CreateIconContent(null);
            Assert.Null(result);
        }

        [Fact]
        public void DrawingImage_WithGeometryDrawing_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var drawing = new GeometryDrawing { Geometry = geometry };
            var drawingImage = new DrawingImage(drawing);
            var result = TabbedPage.CreateIconContent(drawingImage);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void Path_HasStretchUniform()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var result = TabbedPage.CreateIconContent(geometry);
            Assert.Equal(Stretch.Uniform, ((Path)result!).Stretch);
        }

        [Fact]
        public void Image_ReturnsImage()
        {
            var image = new TestImage();
            var result = TabbedPage.CreateIconContent(image);
            Assert.IsType<Image>(result);
            Assert.Same(image, ((Image)result!).Source);
        }

        private sealed class TestImage : IImage
        {
            public Size Size => new Size(1, 1);
            public void Draw(DrawingContext context, Rect sourceRect, Rect destRect) { }
        }

        [Fact]
        public void UnsupportedType_ReturnsNull()
        {
            var result = TabbedPage.CreateIconContent(42);
            Assert.Null(result);
        }
    }

    public class DataTemplateTests : ScopedTestBase
    {
        private record DataItem(string Name);

        [Fact]
        public void CustomPageTemplate_Build_CreatesContentPage_WithCorrectHeader()
        {
            var template = new FuncDataTemplate<DataItem>(
                (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: true);
            var tp = new TabbedPage { PageTemplate = template };

            var built = tp.PageTemplate!.Build(new DataItem("Electronics")) as ContentPage;

            Assert.NotNull(built);
            Assert.Equal("Electronics", built!.Header);
        }

        [Fact]
        public void DefaultPageDataTemplate_IsNonNull()
        {
            Assert.NotNull(new TabbedPage().PageTemplate);
        }

        [Fact]
        public void DefaultPageDataTemplate_WrapsNonPageItem_InContentPage()
        {
            // Data items must be wrapped in ContentPage, not shown as raw ToString() headers.
            var tp = new TabbedPage();
            var built = tp.PageTemplate!.Build(new DataItem("Test"));
            Assert.IsType<ContentPage>(built);
        }

        [Fact]
        public void ViewModelItems_WithPageTemplate_BuildContainersAsContentPages()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("Electronics"),
                new("Books"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var logicals = ((ILogical)tp).LogicalChildren;
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "Electronics");
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "Books");
        }

        [Fact]
        public void ItemsSource_SelectedPage_IsNotNullAfterContainersRealized()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("First"),
                new("Second"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(tp.SelectedPage);
            Assert.Equal(0, tp.SelectedIndex);
            Assert.IsType<ContentPage>(tp.SelectedPage);
            Assert.Equal("First", tp.SelectedPage!.Header?.ToString());
        }

        [Fact]
        public void NonListItemsSource_SelectedPage_And_AutomationName_AreResolved()
        {
            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = EnumerateItems(new("First"), new("Second")),
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(tp.SelectedPage);
            Assert.Equal("First", tp.SelectedPage!.Header?.ToString());
            Assert.Equal("Tab 1 of 2: First", new Avalonia.Automation.Peers.TabbedPageAutomationPeer(tp).GetName());

            tp.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(tp.SelectedPage);
            Assert.Equal("Second", tp.SelectedPage!.Header?.ToString());
            Assert.Equal("Tab 2 of 2: Second", new Avalonia.Automation.Peers.TabbedPageAutomationPeer(tp).GetName());
        }

        [Fact]
        public void ItemsSource_SelectionChanged_ReportsCorrectPage()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("Alpha"),
                new("Beta"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Page? reportedPage = null;
            tp.SelectionChanged += (_, e) => reportedPage = e.CurrentPage;

            tp.SelectedIndex = 1;
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(tp.SelectedPage);
            Assert.Equal(1, tp.SelectedIndex);
            Assert.Equal("Beta", tp.SelectedPage!.Header?.ToString());
            Assert.NotNull(reportedPage);
            Assert.Equal("Beta", reportedPage!.Header?.ToString());
        }

        [Fact]
        public void ViewModelItems_RemovedFromCollection_TemplateCreatedPageRemovedFromLogicalChildren()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("A"),
                new("B"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            items.RemoveAt(1);
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var logicals = ((ILogical)tp).LogicalChildren;
            Assert.DoesNotContain(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "B");
        }

        [Fact]
        public void ItemsSource_Replaced_NoPhantomLogicalChildren()
        {
            var first = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("One"),
                new("Two"),
            };
            var second = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("Three"),
                new("Four"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = first,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            tp.ItemsSource = second;
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var logicals = ((ILogical)tp).LogicalChildren;
            Assert.DoesNotContain(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "One");
            Assert.DoesNotContain(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "Two");
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "Three");
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "Four");
        }

        [Fact]
        public void PageTemplate_ChangedAfterContainersRealized_RebuildsExistingContainers()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("X"),
                new("Y"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = "old-" + item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            tp.PageTemplate = new FuncDataTemplate<DataItem>(
                (item, _) => new ContentPage { Header = "new-" + item!.Name }, supportsRecycling: false);
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var logicals = ((ILogical)tp).LogicalChildren;
            Assert.DoesNotContain(logicals, l => l is ContentPage cp && cp.Header?.ToString()?.StartsWith("old-") == true);
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "new-X");
            Assert.Contains(logicals, l => l is ContentPage cp && cp.Header?.ToString() == "new-Y");
        }

        [Fact]
        public void PageTemplate_ChangedAfterContainersRealized_UpdatesSelectedPage()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("A"),
                new("B"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = "old-" + item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            tp.PageTemplate = new FuncDataTemplate<DataItem>(
                (item, _) => new ContentPage { Header = "new-" + item!.Name }, supportsRecycling: false);
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(tp.SelectedPage);
            Assert.Equal("new-A", tp.SelectedPage!.Header?.ToString());
        }

        [Fact]
        public void ReapplyingTemplate_DoesNotLeavePhantomLogicalChildren()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("A"),
                new("B"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var firstSelectedPage = tp.SelectedPage;

            tp.Template = CreateTabbedPageTemplate();
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(2, ((ILogical)tp).LogicalChildren.Count);
            Assert.DoesNotContain(firstSelectedPage!, ((ILogical)tp).LogicalChildren);
        }

        [Fact]
        public void PageTemplate_SetToNullAfterContainersRealized_ClearsGeneratedPages()
        {
            var items = new System.Collections.ObjectModel.ObservableCollection<DataItem>
            {
                new("A"),
                new("B"),
            };

            var tp = new TabbedPage
            {
                Width = 400, Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => new ContentPage { Header = item!.Name }, supportsRecycling: false),
                Template = CreateTabbedPageTemplate(),
            };

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = tp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var originalSelectedPage = tp.SelectedPage;

            tp.PageTemplate = null;
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Null(tp.SelectedPage);
            Assert.Null(tp.CurrentPage);
            Assert.Empty(((ILogical)tp).LogicalChildren);
            Assert.DoesNotContain(originalSelectedPage!, ((ILogical)tp).LogicalChildren);
        }

        private static FuncControlTemplate<TabbedPage> CreateTabbedPageTemplate()
        {
            return new FuncControlTemplate<TabbedPage>((parent, scope) =>
            {
                var tc = new TabControl
                {
                    Name = "PART_TabControl",
                    Template = new FuncControlTemplate<TabControl>((_, tcScope) =>
                        new ItemsPresenter
                        {
                            Name = "PART_ItemsPresenter",
                        }.RegisterInNameScope(tcScope)),
                };
                tc.RegisterInNameScope(scope);
                return tc;
            });
        }

        private static IEnumerable<DataItem> EnumerateItems(params DataItem[] items)
        {
            foreach (var item in items)
                yield return item;
        }
    }

    public class SwipeGestureTests : ScopedTestBase
    {
        [Fact]
        public void SameGestureId_OnlyAdvancesOneTab()
        {
            var tp = CreateSwipeReadyTabbedPage();

            var firstSwipe = new SwipeGestureEventArgs(7, new Vector(20, 0), default);
            var repeatedSwipe = new SwipeGestureEventArgs(7, new Vector(20, 0), default);

            tp.RaiseEvent(firstSwipe);
            tp.RaiseEvent(repeatedSwipe);
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.True(firstSwipe.Handled);
            Assert.False(repeatedSwipe.Handled);
            Assert.Equal(1, tp.SelectedIndex);
        }

        [Fact]
        public void NewGestureId_CanAdvanceAgain()
        {
            var tp = CreateSwipeReadyTabbedPage();

            tp.RaiseEvent(new SwipeGestureEventArgs(7, new Vector(20, 0), default));
            tp.RaiseEvent(new SwipeGestureEventArgs(8, new Vector(20, 0), default));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(2, tp.SelectedIndex);
        }

        [Fact]
        public void MouseSwipe_Advances_Tab()
        {
            var tp = CreateSwipeReadyTabbedPage();
            var mouse = new MouseTestHelper();

            mouse.Down(tp, position: new Point(200, 100));
            mouse.Move(tp, new Point(160, 100));
            mouse.Up(tp, position: new Point(160, 100));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, tp.SelectedIndex);
        }

        private static TabbedPage CreateSwipeReadyTabbedPage()
        {
            var tp = new TabbedPage
            {
                IsGestureEnabled = true,
                Width = 400,
                Height = 300,
                TabPlacement = TabPlacement.Top,
                SelectedIndex = 0,
                Pages = new AvaloniaList<Page>
                {
                    new ContentPage { Header = "A" },
                    new ContentPage { Header = "B" },
                    new ContentPage { Header = "C" }
                },
                Template = new FuncControlTemplate<TabbedPage>((parent, scope) =>
                {
                    var tabControl = new TabControl
                    {
                        Name = "PART_TabControl",
                        ItemsSource = parent.Pages
                    };
                    scope.Register("PART_TabControl", tabControl);
                    return tabControl;
                })
            };
            tp.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = tp
            };
            tp.ApplyTemplate();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            return tp;
        }
    }

    private sealed class TestableTabbedPage : TabbedPage
    {
        public void CallCommitSelection(int index, Page? page) => CommitSelection(index, page);

        public int CallFindNextEnabledTab(int start, int dir) => FindNextEnabledTab(start, dir);

        protected override void ApplySelectedIndex(int index) => base.ApplySelectedIndex(index);

        public void SimulateKeyDown(Key key)
        {
            var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
            OnKeyDown(e);
        }

        public bool SimulateKeyDownReturnsHandled(Key key)
        {
            var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key };
            OnKeyDown(e);
            return e.Handled;
        }

        public bool SimulateKeyDownWithModifiersReturnsHandled(Key key, KeyModifiers modifiers)
        {
            var e = new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key, KeyModifiers = modifiers };
            OnKeyDown(e);
            return e.Handled;
        }
    }

    public class VisualTreeLifecycleTests : ScopedTestBase
    {
        [Fact]
        public void Detach_And_Reattach_CollectionChangedStillFiresPagesChanged()
        {
            var pages = new AvaloniaList<Page>();
            var tp = new TabbedPage { Pages = pages };
            var root = new TestRoot { Child = tp };

            root.Child = null;
            root.Child = tp;

            int fireCount = 0;
            tp.PagesChanged += (_, _) => fireCount++;

            pages.Add(new ContentPage { Header = "A" });
            pages.Add(new ContentPage { Header = "B" });

            Assert.Equal(2, fireCount);
        }
    }
}
