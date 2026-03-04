using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Media;
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
        [Fact]
        public void BarBackground_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.DodgerBlue);
            var tp = new TabbedPage { BarBackground = brush };
            Assert.Same(brush, tp.BarBackground);
        }

        [Fact]
        public void BarForeground_RoundTrips()
        {
            var brush = Brushes.White;
            var tp = new TabbedPage { BarForeground = brush };
            Assert.Same(brush, tp.BarForeground);
        }

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

        [Fact]
        public void SelectedTabBrush_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.Crimson);
            var tp = new TabbedPage { SelectedTabBrush = brush };
            Assert.Same(brush, tp.SelectedTabBrush);
        }

        [Fact]
        public void UnselectedTabBrush_RoundTrips()
        {
            var brush = new SolidColorBrush(Colors.Gray);
            var tp = new TabbedPage { UnselectedTabBrush = brush };
            Assert.Same(brush, tp.UnselectedTabBrush);
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
        public void CommitSelection_FiresDisappearing_OnPreviousPage()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            bool fired = false;
            page1.Disappearing += (_, _) => fired = true;
            tp.CallCommitSelection(1, page2);

            Assert.True(fired);
        }

        [Fact]
        public void CommitSelection_FiresAppearing_OnNewPage()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            bool fired = false;
            page2.Appearing += (_, _) => fired = true;
            tp.CallCommitSelection(1, page2);

            Assert.True(fired);
        }

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
        public void CommitSelection_LifecycleOrder_DisappearingNavigatedFromNavigatedToAppearing()
        {
            var tp = new TestableTabbedPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            tp.CallCommitSelection(0, page1);

            var order = new List<string>();
            page1.Disappearing += (_, _) => order.Add("Disappearing");
            page1.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
            page2.NavigatedTo += (_, _) => order.Add("NavigatedTo");
            page2.Appearing += (_, _) => order.Add("Appearing");
            tp.CallCommitSelection(1, page2);

            Assert.Equal(new[] { "Disappearing", "NavigatedFrom", "NavigatedTo", "Appearing" }, order);
        }

        [Fact]
        public void CommitSelection_SamePage_NoLifecycleEvents()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };
            tp.CallCommitSelection(0, page);

            var events = new List<string>();
            page.Appearing += (_, _) => events.Add("Appearing");
            page.Disappearing += (_, _) => events.Add("Disappearing");
            tp.CallCommitSelection(0, page);

            Assert.Empty(events);
        }

        [Fact]
        public void CommitSelection_FirstPage_NoDisappearing_NavigatedToHasNullPrevious()
        {
            // On first selection there is no previous page, so only NavigatedTo+Appearing
            // should fire (with PreviousPage == null) and nothing on a nonexistent previous.
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };

            NavigatedToEventArgs? navigatedToArgs = null;
            var events = new List<string>();
            page.NavigatedTo  += (_, e) => { navigatedToArgs = e; events.Add("NavigatedTo"); };
            page.Appearing    += (_, _) => events.Add("Appearing");
            page.Disappearing += (_, _) => events.Add("Disappearing");

            tp.CallCommitSelection(0, page);

            Assert.Equal(new[] { "NavigatedTo", "Appearing" }, events);
            Assert.NotNull(navigatedToArgs);
            Assert.Null(navigatedToArgs!.PreviousPage);
            Assert.Equal(NavigationType.Replace, navigatedToArgs.NavigationType);
        }

        [Fact]
        public void CommitSelection_ToNull_FiresDisappearingAndNavigatedFrom_WithNullDestination()
        {
            var tp = new TestableTabbedPage();
            var page = new ContentPage { Header = "A" };
            tp.CallCommitSelection(0, page);

            NavigatedFromEventArgs? navigatedFromArgs = null;
            var events = new List<string>();
            page.Disappearing  += (_, _)  => events.Add("Disappearing");
            page.NavigatedFrom += (_, e)  => { navigatedFromArgs = e; events.Add("NavigatedFrom"); };
            page.Appearing     += (_, _)  => events.Add("Appearing");

            tp.CallCommitSelection(-1, null);

            Assert.Equal(new[] { "Disappearing", "NavigatedFrom" }, events);
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

    public class CreateIconControlTests : ScopedTestBase
    {
        [Fact]
        public void Geometry_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var result = TabbedPage.CreateIconControl(geometry);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void PathIcon_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var pathIcon = new PathIcon { Data = geometry };
            var result = TabbedPage.CreateIconControl(pathIcon);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void EmptyString_ReturnsNull()
        {
            var result = TabbedPage.CreateIconControl("");
            Assert.Null(result);
        }

        [Fact]
        public void NullString_ReturnsNull()
        {
            var result = TabbedPage.CreateIconControl((string?)null);
            Assert.Null(result);
        }

        [Fact]
        public void Null_ReturnsNull()
        {
            var result = TabbedPage.CreateIconControl(null);
            Assert.Null(result);
        }

        [Fact]
        public void DrawingImage_WithGeometryDrawing_ReturnsPath()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var drawing = new GeometryDrawing { Geometry = geometry };
            var drawingImage = new DrawingImage(drawing);
            var result = TabbedPage.CreateIconControl(drawingImage);
            Assert.IsType<Path>(result);
            Assert.Same(geometry, ((Path)result!).Data);
        }

        [Fact]
        public void Path_HasStretchUniform()
        {
            var geometry = new EllipseGeometry { Rect = new Rect(0, 0, 10, 10) };
            var result = TabbedPage.CreateIconControl(geometry);
            Assert.Equal(Stretch.Uniform, ((Path)result!).Stretch);
        }

        [Fact]
        public void UnsupportedType_ReturnsNull()
        {
            var result = TabbedPage.CreateIconControl(42);
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
    }

    public class ForegroundResourcesTests : ScopedTestBase
    {
        private static (TabbedPage tp, TabControl tc) Create()
        {
            var tabControl = new TabControl();
            var tp = new TabbedPage
            {
                Template = new FuncControlTemplate<TabbedPage>((_, scope) =>
                {
                    scope.Register("PART_TabControl", tabControl);
                    return tabControl;
                })
            };
            tp.ApplyTemplate();
            return (tp, tabControl);
        }

        [Fact]
        public void BarForeground_DoesNotManipulateTabControlResources()
        {
            // Foreground colors are now driven by XAML ancestor bindings, not resource overrides.
            var (tp, tc) = Create();
            tp.BarForeground = Brushes.White;
            Assert.False(tc.Resources.TryGetResource("TabbedPageTabItemHeaderForegroundSelected", null, out _));
            Assert.False(tc.Resources.TryGetResource("TabbedPageTabItemHeaderForegroundUnselected", null, out _));
        }
    }

    private sealed class TestableTabbedPage : TabbedPage
    {
        public void CallCommitSelection(int index, Page? page) => CommitSelection(index, page);

        public int CallFindNextEnabledTab(int start, int dir) => FindNextEnabledTab(start, dir);

        protected override void ApplySelectedIndex(int index) => base.ApplySelectedIndex(index);
    }
}
