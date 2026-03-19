using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class CarouselPageTests
{
    public class PropertyDefaults : ScopedTestBase
    {
        [Fact]
        public void SelectedIndex_DefaultIsMinusOne()
        {
            var cp = new CarouselPage();
            Assert.Equal(-1, cp.SelectedIndex);
        }

        [Fact]
        public void SelectedPage_DefaultIsNull()
        {
            var cp = new CarouselPage();
            Assert.Null(cp.SelectedPage);
        }

        [Fact]
        public void CurrentPage_DefaultIsNull()
        {
            var cp = new CarouselPage();
            Assert.Null(cp.CurrentPage);
        }

        [Fact]
        public void PageTransition_DefaultIsNull()
        {
            var cp = new CarouselPage();
            Assert.Null(cp.PageTransition);
        }

        [Fact]
        public void IsGestureEnabled_DefaultIsTrue()
        {
            var cp = new CarouselPage();
            Assert.True(cp.IsGestureEnabled);
        }

        [Fact]
        public void IsKeyboardNavigationEnabled_DefaultIsTrue()
        {
            var cp = new CarouselPage();
            Assert.True(cp.IsKeyboardNavigationEnabled);
        }

    }

    public class PropertyRoundTrips : ScopedTestBase
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsGestureEnabled_RoundTrips(bool value)
        {
            var cp = new CarouselPage { IsGestureEnabled = value };
            Assert.Equal(value, cp.IsGestureEnabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsKeyboardNavigationEnabled_RoundTrips(bool value)
        {
            var cp = new CarouselPage { IsKeyboardNavigationEnabled = value };
            Assert.Equal(value, cp.IsKeyboardNavigationEnabled);
        }

        [Fact]
        public void PageTransition_RoundTrip()
        {
            var transition = new TestPageTransition();
            var cp = new CarouselPage { PageTransition = transition };
            Assert.Same(transition, cp.PageTransition);
        }

        [Fact]
        public void PageTransition_CanBeSetToNull()
        {
            var cp = new CarouselPage { PageTransition = new TestPageTransition() };
            cp.PageTransition = null;
            Assert.Null(cp.PageTransition);
        }

        [Fact]
        public void ItemsPanel_RoundTrip()
        {
            var template = new FuncTemplate<Panel?>(() => new StackPanel());
            var cp = new CarouselPage { ItemsPanel = template };
            Assert.Same(template, cp.ItemsPanel);
        }

        [Fact]
        public void PageTemplate_RoundTrip()
        {
            var template = new FuncDataTemplate<Page>((_, _) => new ContentControl());
            var cp = new CarouselPage { PageTemplate = template };
            Assert.Same(template, cp.PageTemplate);
        }

        [Fact]
        public void PageTemplate_CanBeSetToNull()
        {
            var cp = new CarouselPage { PageTemplate = null };
            Assert.Null(cp.PageTemplate);
        }
    }

    public class SelectionBehavior : ScopedTestBase
    {
        [Fact]
        public void SelectedIndex_SetUpdatesSelectedPageAndCurrentPage()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            cp.SelectedIndex = 1;

            Assert.Same(page2, cp.SelectedPage);
            Assert.Same(page2, cp.CurrentPage);
        }

        [Fact]
        public void SelectedIndex_AutoSelectsFirst_WhenPagesSet()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });

            Assert.Equal(0, cp.SelectedIndex);
            Assert.Same(page1, cp.SelectedPage);
        }

        [Fact]
        public void UpdateActivePage_SelectsFirst_WhenFirstPageAddedToEmptyCollection()
        {
            var cp = new CarouselPage();
            Assert.Equal(-1, cp.SelectedIndex);

            var page1 = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page1);

            Assert.Equal(0, cp.SelectedIndex);
            Assert.Same(page1, cp.SelectedPage);
        }

        [Fact]
        public void UpdateActivePage_DoesNotOverrideExistingSelection()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 1;

            ((AvaloniaList<Page>)cp.Pages!).Add(new ContentPage { Header = "C" });

            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void SelectedPage_SameReferenceAsCurrentPage()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 1;

            Assert.Same(cp.SelectedPage, cp.CurrentPage);
        }

        [Fact]
        public void SelectedIndex_InvalidWithPages_CoercesToFirstPage()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });

            cp.SelectedIndex = 999;

            Assert.Equal(0, cp.SelectedIndex);
            Assert.Same(page1, cp.SelectedPage);
            Assert.Same(page1, cp.CurrentPage);
        }

        [Fact]
        public void SelectedIndex_SetBeforePages_IsStored()
        {
            var cp = new CarouselPage();

            cp.SelectedIndex = 2;

            Assert.Equal(2, cp.SelectedIndex);
            Assert.Null(cp.SelectedPage);
            Assert.Null(cp.CurrentPage);
        }

        [Fact]
        public void SelectedIndex_SetBeforePages_IsAppliedWhenPagesSet()
        {
            var cp = new CarouselPage
            {
                SelectedIndex = 2
            };

            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            var page3 = new ContentPage { Header = "C" };

            cp.Pages = new AvaloniaList<Page> { page1, page2, page3 };

            Assert.Equal(2, cp.SelectedIndex);
            Assert.Same(page3, cp.SelectedPage);
            Assert.Same(page3, cp.CurrentPage);
        }

        [Fact]
        public void Pages_SetNull_SelectedIndex_RetainsLastValue()
        {
            var cp = new CarouselPage();
            ((AvaloniaList<Page>)cp.Pages!).Add(new ContentPage { Header = "A" });
            Assert.Equal(0, cp.SelectedIndex);

            cp.Pages = null;

            Assert.Equal(0, cp.SelectedIndex);
        }
    }

    public class SelectionChangedEvent : ScopedTestBase
    {
        [Fact]
        public void SelectionChanged_FiresWhenSelectionChanges()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            PageSelectionChangedEventArgs? received = null;
            cp.SelectionChanged += (_, e) => received = e;

            cp.SelectedIndex = 1;

            Assert.NotNull(received);
            Assert.Same(page1, received!.PreviousPage);
            Assert.Same(page2, received.CurrentPage);
        }

        [Fact]
        public void SelectionChanged_NotFiredWhenSamePageSelected()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page);
            cp.SelectedIndex = 0;

            int count = 0;
            cp.SelectionChanged += (_, _) => count++;

            cp.SelectedIndex = 0;

            Assert.Equal(0, count);
        }

        [Fact]
        public void SelectionChanged_TracksSequentialSelections()
        {
            var cp = new CarouselPage();
            var pages = new[]
            {
                new ContentPage { Header = "A" },
                new ContentPage { Header = "B" },
                new ContentPage { Header = "C" },
            };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(pages);
            cp.SelectedIndex = 0;

            var events = new List<(Page? prev, Page? curr)>();
            cp.SelectionChanged += (_, e) => events.Add((e.PreviousPage, e.CurrentPage));

            cp.SelectedIndex = 1;
            cp.SelectedIndex = 2;
            cp.SelectedIndex = 0;

            Assert.Equal(3, events.Count);
            Assert.Same(pages[0], events[0].prev);
            Assert.Same(pages[1], events[0].curr);
            Assert.Same(pages[1], events[1].prev);
            Assert.Same(pages[2], events[1].curr);
            Assert.Same(pages[2], events[2].prev);
            Assert.Same(pages[0], events[2].curr);
        }

        [Fact]
        public void SelectionChanged_PreviousPage_IsNull_OnFirstAutoSelection()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };

            PageSelectionChangedEventArgs? received = null;
            cp.SelectionChanged += (_, e) => received = e;

            ((AvaloniaList<Page>)cp.Pages!).Add(page1);

            Assert.NotNull(received);
            Assert.Null(received!.PreviousPage);
            Assert.Same(page1, received.CurrentPage);
        }
    }

    public class CurrentPageChangedEvent : ScopedTestBase
    {
        [Fact]
        public void CurrentPageChanged_FiresOnSelectionChange()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            int count = 0;
            cp.CurrentPageChanged += (_, _) => count++;

            cp.SelectedIndex = 1;

            Assert.Equal(1, count);
        }

        [Fact]
        public void CurrentPageChanged_NotFiredWhenSamePageSelected()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page);
            cp.SelectedIndex = 0;

            int count = 0;
            cp.CurrentPageChanged += (_, _) => count++;

            cp.SelectedIndex = 0;

            Assert.Equal(0, count);
        }
    }

    public class PagesChangedEvent : ScopedTestBase
    {
        [Fact]
        public void PagesChanged_FiresOnAdd()
        {
            var cp = new CarouselPage();
            NotifyCollectionChangedEventArgs? received = null;
            cp.PagesChanged += (_, e) => received = e;

            ((AvaloniaList<Page>)cp.Pages!).Add(new ContentPage());

            Assert.NotNull(received);
            Assert.Equal(NotifyCollectionChangedAction.Add, received!.Action);
        }

        [Fact]
        public void PagesChanged_FiresOnRemove()
        {
            var cp = new CarouselPage();
            var page = new ContentPage();
            ((AvaloniaList<Page>)cp.Pages!).Add(page);

            NotifyCollectionChangedEventArgs? received = null;
            cp.PagesChanged += (_, e) => received = e;

            ((AvaloniaList<Page>)cp.Pages!).Remove(page);

            Assert.NotNull(received);
            Assert.Equal(NotifyCollectionChangedAction.Remove, received!.Action);
        }

        [Fact]
        public void PagesChanged_NotFiredAfterPagesCollectionReplaced()
        {
            var cp = new CarouselPage();
            var oldPages = (AvaloniaList<Page>)cp.Pages!;
            cp.Pages = new AvaloniaList<Page>();

            cp.PagesChanged += (_, _) => throw new InvalidOperationException("Should not fire for old collection");

            oldPages.Add(new ContentPage());
        }
    }

    public class PageLifecycleEvents : ScopedTestBase
    {
        [Fact]
        public void SelectionChange_FiresNavigatedFrom_OnPreviousPage()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            NavigatedFromEventArgs? args = null;
            page1.NavigatedFrom += (_, e) => args = e;

            cp.SelectedIndex = 1;

            Assert.NotNull(args);
            Assert.Same(page2, args!.DestinationPage);
        }

        [Fact]
        public void SelectionChange_FiresNavigatedTo_OnNewPage()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            NavigatedToEventArgs? args = null;
            page2.NavigatedTo += (_, e) => args = e;

            cp.SelectedIndex = 1;

            Assert.NotNull(args);
            Assert.Same(page1, args!.PreviousPage);
        }

        [Fact]
        public void SelectionChange_LifecycleOrder_NavigatedFromThenNavigatedTo()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });
            cp.SelectedIndex = 0;

            var order = new List<string>();
            page1.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
            page2.NavigatedTo += (_, _) => order.Add("NavigatedTo");

            cp.SelectedIndex = 1;

            Assert.Equal(new[] { "NavigatedFrom", "NavigatedTo" }, order);
        }

        [Fact]
        public void SelectionChange_SamePage_NoLifecycleEvents()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page);
            cp.SelectedIndex = 0;

            var events = new List<string>();
            page.NavigatedTo += (_, _) => events.Add("NavigatedTo");
            page.NavigatedFrom += (_, _) => events.Add("NavigatedFrom");

            cp.SelectedIndex = 0;

            Assert.Empty(events);
        }

        [Fact]
        public void NavigatedFrom_NavigationType_IsReplace()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });

            NavigatedFromEventArgs? args = null;
            page1.NavigatedFrom += (_, e) => args = e;

            cp.SelectedIndex = 1;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }

        [Fact]
        public void NavigatedTo_NavigationType_IsReplace()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });

            NavigatedToEventArgs? args = null;
            page2.NavigatedTo += (_, e) => args = e;

            cp.SelectedIndex = 1;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }

        [Fact]
        public void NavigatedTo_FirstAutoSelection_NavigationType_IsReplace()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };

            NavigatedToEventArgs? args = null;
            page.NavigatedTo += (_, e) => args = e;

            ((AvaloniaList<Page>)cp.Pages!).Add(page);

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }
    }

    public class LogicalChildrenTests : ScopedTestBase
    {
        [Fact]
        public void Pages_Added_BecomeLogicalChildren()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };

            ((AvaloniaList<Page>)cp.Pages!).Add(page);

            Assert.Contains(page, cp.GetLogicalChildren());
        }

        [Fact]
        public void Pages_Removed_RemovedFromLogicalChildren()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page);

            ((AvaloniaList<Page>)cp.Pages!).Remove(page);

            Assert.DoesNotContain(page, cp.GetLogicalChildren());
        }

        [Fact]
        public void Pages_Clear_RemovesAllLogicalChildren()
        {
            var cp = new CarouselPage();
            var page1 = new ContentPage { Header = "A" };
            var page2 = new ContentPage { Header = "B" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { page1, page2 });

            ((AvaloniaList<Page>)cp.Pages!).Clear();

            Assert.DoesNotContain(page1, cp.GetLogicalChildren());
            Assert.DoesNotContain(page2, cp.GetLogicalChildren());
        }

        [Fact]
        public void Pages_Replaced_OldPagesRemovedNewPagesAdded()
        {
            var cp = new CarouselPage();
            var old1 = new ContentPage { Header = "Old1" };
            var old2 = new ContentPage { Header = "Old2" };
            ((AvaloniaList<Page>)cp.Pages!).AddRange(new[] { old1, old2 });

            var newPages = new AvaloniaList<Page> { new ContentPage { Header = "New1" } };
            cp.Pages = newPages;

            Assert.DoesNotContain(old1, cp.GetLogicalChildren());
            Assert.DoesNotContain(old2, cp.GetLogicalChildren());
            Assert.Contains(newPages[0], cp.GetLogicalChildren());
        }

        [Fact]
        public void Pages_SetNull_ClearsLogicalChildren()
        {
            var cp = new CarouselPage();
            var page = new ContentPage { Header = "A" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page);

            cp.Pages = null;

            Assert.DoesNotContain(page, cp.GetLogicalChildren());
        }
    }

    public class KeyboardNavigationTests : ScopedTestBase
    {
        [Fact]
        public void RightKey_NavigatesToNextPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.SimulateKeyDown(Key.Right);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void DownKey_NavigatesToNextPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.SimulateKeyDown(Key.Down);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void LeftKey_NavigatesToPreviousPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.SimulateKeyDown(Key.Left);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void UpKey_NavigatesToPreviousPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.SimulateKeyDown(Key.Up);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void HomeKey_NavigatesToFirstPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.SimulateKeyDown(Key.Home);
            Assert.Equal(0, cp.SelectedIndex);
        }

        [Fact]
        public void EndKey_NavigatesToLastPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.SimulateKeyDown(Key.End);
            Assert.Equal(2, cp.SelectedIndex);
        }

        [Fact]
        public void LeftKey_AtFirstPage_DoesNotNavigate()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.SimulateKeyDown(Key.Left);
            Assert.Equal(0, cp.SelectedIndex);
        }

        [Fact]
        public void RightKey_AtLastPage_DoesNotNavigate()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.SimulateKeyDown(Key.Right);
            Assert.Equal(2, cp.SelectedIndex);
        }

        [Fact]
        public void KeyNavigation_Disabled_IgnoresArrowKey()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.IsKeyboardNavigationEnabled = false;
            cp.SimulateKeyDown(Key.Right);
            Assert.Equal(0, cp.SelectedIndex);
        }

        [Fact]
        public void RightKey_MarksEventHandled()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            var handled = cp.SimulateKeyDownReturnsHandled(Key.Right);
            Assert.True(handled);
        }

        [Fact]
        public void RightKey_AtLastPage_DoesNotMarkEventHandled()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            var handled = cp.SimulateKeyDownReturnsHandled(Key.Right);
            Assert.False(handled);
        }

        [Fact]
        public void RtlFlowDirection_LeftKey_NavigatesToNextPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.FlowDirection = FlowDirection.RightToLeft;
            cp.SimulateKeyDown(Key.Left);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void RtlFlowDirection_RightKey_NavigatesToPreviousPage()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.FlowDirection = FlowDirection.RightToLeft;
            cp.SimulateKeyDown(Key.Right);
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void RtlFlowDirection_LeftKey_AtLastPage_DoesNotNavigate()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.FlowDirection = FlowDirection.RightToLeft;
            cp.SimulateKeyDown(Key.Left);
            Assert.Equal(2, cp.SelectedIndex);
        }

        [Fact]
        public void RtlFlowDirection_RightKey_AtFirstPage_DoesNotNavigate()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.FlowDirection = FlowDirection.RightToLeft;
            cp.SimulateKeyDown(Key.Right);
            Assert.Equal(0, cp.SelectedIndex);
        }

        private static TestableCarouselPage MakeCarousel(int count, int selectedIndex)
        {
            var cp = new TestableCarouselPage();
            for (var i = 0; i < count; i++)
                ((AvaloniaList<Page>)cp.Pages!).Add(new ContentPage { Header = $"P{i}" });
            cp.SelectedIndex = selectedIndex;
            return cp;
        }
    }

    public class WheelBehavior : ScopedTestBase
    {
        [Fact]
        public void WheelDown_NavigatesForward()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.SimulateWheel(new Vector(0, -1));
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void WheelUp_NavigatesBackward()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            cp.SimulateWheel(new Vector(0, 1));
            Assert.Equal(1, cp.SelectedIndex);
        }

        [Fact]
        public void WheelDown_AtLastPage_DoesNotHandleEvent()
        {
            var cp = MakeCarousel(3, selectedIndex: 2);
            var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
            Assert.Equal(2, cp.SelectedIndex);
            Assert.False(handled);
        }

        [Fact]
        public void WheelUp_AtFirstPage_DoesNotHandleEvent()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            var handled = cp.SimulateWheelReturnsHandled(new Vector(0, 1));
            Assert.Equal(0, cp.SelectedIndex);
            Assert.False(handled);
        }

        [Fact]
        public void Wheel_WhenGestureDisabled_DoesNotHandleEvent()
        {
            var cp = MakeCarousel(3, selectedIndex: 0);
            cp.IsGestureEnabled = false;
            var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
            Assert.Equal(0, cp.SelectedIndex);
            Assert.False(handled);
        }

        [Fact]
        public void Wheel_HandledByChild_DoesNotNavigate()
        {
            var cp = new CarouselPage
            {
                Width = 400,
                Height = 300,
                IsGestureEnabled = true,
                Template = CreateCarouselPageTemplate(),
            };

            var child = new Border();
            child.AddHandler(InputElement.PointerWheelChangedEvent, (_, e) => e.Handled = true);

            var page0 = new ContentPage { Header = "P0", Content = child };
            var page1 = new ContentPage { Header = "P1" };
            var page2 = new ContentPage { Header = "P2" };
            ((AvaloniaList<Page>)cp.Pages!).Add(page0);
            ((AvaloniaList<Page>)cp.Pages!).Add(page1);
            ((AvaloniaList<Page>)cp.Pages!).Add(page2);

            var root = new TestRoot(cp) { ClientSize = new Size(400, 300) };
            root.LayoutManager.ExecuteInitialLayoutPass();

            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
            var wheelArgs = new PointerWheelEventArgs(
                child,
                pointer,
                root,
                default,
                0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
                KeyModifiers.None,
                new Vector(0, -1))
            {
                RoutedEvent = InputElement.PointerWheelChangedEvent
            };
            child.RaiseEvent(wheelArgs);

            Assert.True(wheelArgs.Handled);
            Assert.Equal(0, cp.SelectedIndex);
        }

        private static FuncControlTemplate<CarouselPage> CreateCarouselPageTemplate()
        {
            return new FuncControlTemplate<CarouselPage>((_, scope) =>
                new Carousel
                {
                    Name = "PART_Carousel",
                    Template = new FuncControlTemplate((c, ns) =>
                        new ScrollViewer
                        {
                            Name = "PART_ScrollViewer",
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                            Content = new ItemsPresenter
                            {
                                Name = "PART_ItemsPresenter",
                                [~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty],
                            }.RegisterInNameScope(ns)
                        }.RegisterInNameScope(ns)),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }.RegisterInNameScope(scope));
        }

        private static TestableCarouselPage MakeCarousel(int count, int selectedIndex)
        {
            var cp = new TestableCarouselPage();
            for (var i = 0; i < count; i++)
                ((AvaloniaList<Page>)cp.Pages!).Add(new ContentPage { Header = $"P{i}" });
            cp.SelectedIndex = selectedIndex;
            _ = new TestRoot { Child = cp };
            return cp;
        }
    }

    public class DataTemplateTests : ScopedTestBase
    {
        private record DataItem(string Name);

        [Fact]
        public void ItemsSource_SelectedPage_IsResolvedAfterLayout()
        {
            var cp = CreateTemplatedCarouselPage<CarouselPage>(
                new[] { new DataItem("First"), new DataItem("Second") });

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.NotNull(cp.SelectedPage);
            Assert.Equal("First", cp.SelectedPage!.Header?.ToString());
            Assert.Equal("Page 1 of 2: First", Avalonia.Automation.AutomationProperties.GetName(cp));
        }

        [Fact]
        public void KeyboardNavigation_UsesItemsSourceCount()
        {
            var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
                new[] { new DataItem("First"), new DataItem("Second") });

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            cp.SimulateKeyDown(Key.Right);
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, cp.SelectedIndex);
            Assert.NotNull(cp.SelectedPage);
            Assert.Equal("Second", cp.SelectedPage!.Header?.ToString());
            Assert.Equal("Page 2 of 2: Second", Avalonia.Automation.AutomationProperties.GetName(cp));
        }

        [Fact]
        public void WheelNavigation_UsesItemsSourceCount()
        {
            var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
                new[] { new DataItem("First"), new DataItem("Second") });

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var handled = cp.SimulateWheelReturnsHandled(new Vector(0, -1));
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.True(handled);
            Assert.Equal(1, cp.SelectedIndex);
            Assert.NotNull(cp.SelectedPage);
            Assert.Equal("Second", cp.SelectedPage!.Header?.ToString());
        }

        [Fact]
        public void ItemsSource_SelectionChanges_FireLifecycleOnGeneratedPages()
        {
            var cp = CreateTemplatedCarouselPage<TestableCarouselPage>(
                new[] { new DataItem("First"), new DataItem("Second") },
                item => new TrackingPage
                {
                    Header = item?.Name,
                    Content = new Border { Width = 400, Height = 300 }
                });

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var firstPage = Assert.IsType<TrackingPage>(cp.SelectedPage);
            Assert.Equal(1, firstPage.NavigatedToCount);
            Assert.Equal(0, firstPage.NavigatedFromCount);

            cp.SimulateKeyDown(Key.Right);
            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var secondPage = Assert.IsType<TrackingPage>(cp.SelectedPage);
            Assert.Equal(1, firstPage.NavigatedFromCount);
            Assert.Equal(1, secondPage.NavigatedToCount);
            Assert.Same(secondPage, cp.CurrentPage);
        }

        [Fact]
        public void PageTemplate_ChangedAfterContainersRealized_UpdatesSelectedPage()
        {
            var cp = CreateTemplatedCarouselPage<CarouselPage>(
                new[] { new DataItem("First"), new DataItem("Second") },
                item => new ContentPage
                {
                    Header = $"Detail {item?.Name}",
                    Content = new Border { Width = 400, Height = 300 }
                });

            var root = new TestRoot { ClientSize = new Size(400, 300), Child = cp };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var originalPage = Assert.IsType<ContentPage>(cp.SelectedPage);
            Assert.Equal("Detail First", originalPage.Header);

            cp.PageTemplate = new FuncDataTemplate<DataItem>(
                (item, _) => new ContentPage
                {
                    Header = $"Showcase {item?.Name}",
                    Content = new Border { Width = 400, Height = 300 }
                },
                supportsRecycling: false);

            root.LayoutManager.ExecuteLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var updatedPage = Assert.IsType<ContentPage>(cp.SelectedPage);
            Assert.NotSame(originalPage, updatedPage);
            Assert.Equal("Showcase First", updatedPage.Header);
        }

        private static T CreateTemplatedCarouselPage<T>(IEnumerable<DataItem> items)
            where T : CarouselPage, new()
        {
            return CreateTemplatedCarouselPage<T>(
                items,
                item => new ContentPage
                {
                    Header = item?.Name,
                    Content = new Border { Width = 400, Height = 300 }
                });
        }

        private static T CreateTemplatedCarouselPage<T>(IEnumerable<DataItem> items, Func<DataItem?, Page> pageFactory)
            where T : CarouselPage, new()
        {
            return new T
            {
                Width = 400,
                Height = 300,
                ItemsSource = items,
                PageTemplate = new FuncDataTemplate<DataItem>(
                    (item, _) => pageFactory(item),
                    supportsRecycling: false),
                Template = CreateCarouselPageTemplate(),
            };
        }

        private static FuncControlTemplate<CarouselPage> CreateCarouselPageTemplate()
        {
            return new FuncControlTemplate<CarouselPage>((_, scope) =>
                new Carousel
                {
                    Name = "PART_Carousel",
                    Template = CarouselTemplate(),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                }.RegisterInNameScope(scope));
        }

        private static IControlTemplate CarouselTemplate()
        {
            return new FuncControlTemplate((c, ns) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty],
                    }.RegisterInNameScope(ns)
                }.RegisterInNameScope(ns));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate((_, ns) =>
                new ScrollContentPresenter
                {
                    Name = "PART_ContentPresenter",
                }.RegisterInNameScope(ns));
        }

        private sealed class TrackingPage : ContentPage
        {
            public int NavigatedToCount { get; private set; }
            public int NavigatedFromCount { get; private set; }

            protected override void OnNavigatedTo(NavigatedToEventArgs args)
            {
                NavigatedToCount++;
                base.OnNavigatedTo(args);
            }

            protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
            {
                NavigatedFromCount++;
                base.OnNavigatedFrom(args);
            }
        }
    }

    private sealed class TestableCarouselPage : CarouselPage
    {
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

        public void SimulateWheel(Vector delta)
        {
            SimulateWheelReturnsHandled(delta);
        }

        public bool SimulateWheelReturnsHandled(Vector delta)
        {
            var pointer = new FakePointer();
            var e = new PointerWheelEventArgs(this, pointer, this, default, 0,
                new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other),
                KeyModifiers.None, delta)
            {
                RoutedEvent = PointerWheelChangedEvent
            };
            RaiseEvent(e);
            return e.Handled;
        }
    }

    public class SwipeGestureTests : ScopedTestBase
    {
        [Fact]
        public void MouseSwipe_Advances_Page()
        {
            var clock = new MockGlobalClock();
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(globalClock: clock));
            using var sync = UnitTestSynchronizationContext.Begin();

            var (cp, carousel, panel) = CreateSwipeReadyCarouselPage();
            var mouse = new MouseTestHelper();

            mouse.Down(panel, position: new Point(200, 100));
            mouse.Move(panel, new Point(40, 100));
            mouse.Up(panel, position: new Point(40, 100));
            clock.Pulse(TimeSpan.Zero);
            clock.Pulse(TimeSpan.FromSeconds(1));
            sync.ExecutePostedCallbacks();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, carousel.SelectedIndex);
            Assert.Equal(1, cp.SelectedIndex);
            Assert.Same(((AvaloniaList<Page>)cp.Pages!)[1], cp.CurrentPage);
        }

        [Fact]
        public void TouchSwipe_Advances_Page()
        {
            var clock = new MockGlobalClock();
            using var app = UnitTestApplication.Start(
                TestServices.MockPlatformRenderInterface.With(globalClock: clock));
            using var sync = UnitTestSynchronizationContext.Begin();

            var (cp, carousel, panel) = CreateSwipeReadyCarouselPage();
            var touch = new TouchTestHelper();

            touch.Down(panel, new Point(200, 100));
            touch.Move(panel, new Point(40, 100));
            touch.Up(panel, new Point(40, 100));
            clock.Pulse(TimeSpan.Zero);
            clock.Pulse(TimeSpan.FromSeconds(1));
            sync.ExecutePostedCallbacks();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, carousel.SelectedIndex);
            Assert.Equal(1, cp.SelectedIndex);
            Assert.Same(((AvaloniaList<Page>)cp.Pages!)[1], cp.CurrentPage);
        }

        private static (CarouselPage Page, Carousel Carousel, VirtualizingCarouselPanel Panel) CreateSwipeReadyCarouselPage()
        {
            var cp = new CarouselPage
            {
                Width = 400,
                Height = 300,
                IsGestureEnabled = true,
                PageTransition = new PageSlide(TimeSpan.FromMilliseconds(1)),
                Pages = new AvaloniaList<Page>
                {
                    new ContentPage { Header = "A", Content = new Border { Width = 400, Height = 300 } },
                    new ContentPage { Header = "B", Content = new Border { Width = 400, Height = 300 } },
                    new ContentPage { Header = "C", Content = new Border { Width = 400, Height = 300 } }
                },
                Template = new FuncControlTemplate<CarouselPage>((parent, scope) =>
                    new Carousel
                    {
                        Name = "PART_Carousel",
                        Template = CarouselTemplate(),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        [~ItemsControl.ItemsSourceProperty] = parent[~CarouselPage.PagesProperty],
                        [~ItemsControl.ItemTemplateProperty] = parent[~CarouselPage.PageTemplateProperty],
                        [~ItemsControl.ItemsPanelProperty] = parent[~CarouselPage.ItemsPanelProperty],
                        [~Carousel.PageTransitionProperty] = parent[~CarouselPage.PageTransitionProperty],
                    }.RegisterInNameScope(scope))
            };

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = cp
            };
            root.ExecuteInitialLayoutPass();
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            var carousel = cp.GetVisualDescendants().OfType<Carousel>().Single();
            var panel = Assert.IsType<VirtualizingCarouselPanel>(carousel.Presenter!.Panel!);
            Assert.True(carousel.IsSwipeEnabled);
            var recognizer = Assert.Single(panel.GestureRecognizers.OfType<SwipeGestureRecognizer>());
            Assert.True(recognizer.IsEnabled);
            Assert.True(recognizer.CanHorizontallySwipe);
            recognizer.IsMouseEnabled = true;
            return (cp, carousel, panel);
        }

        private static IControlTemplate CarouselTemplate()
        {
            return new FuncControlTemplate((c, ns) =>
                new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    Template = ScrollViewerTemplate(),
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                    Content = new ItemsPresenter
                    {
                        Name = "PART_ItemsPresenter",
                        [~ItemsPresenter.ItemsPanelProperty] = c[~ItemsControl.ItemsPanelProperty],
                    }.RegisterInNameScope(ns)
                }.RegisterInNameScope(ns));
        }

        private static FuncControlTemplate ScrollViewerTemplate()
        {
            return new FuncControlTemplate<ScrollViewer>((parent, scope) =>
                new Panel
                {
                    Children =
                    {
                        new ScrollContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                        }.RegisterInNameScope(scope),
                    }
                });
        }
    }

    public class InteractiveTransitionTests : ScopedTestBase
    {
        [Fact]
        public void PageSlide_Update_AppliesTranslateTransformToFrom()
        {
            var parent = new Canvas { Width = 400, Height = 300 };
            var from = new Border();
            var to = new Border();
            parent.Children.Add(from);
            parent.Children.Add(to);
            parent.Measure(new Size(400, 300));
            parent.Arrange(new Rect(0, 0, 400, 300));

            var slide = new PageSlide(TimeSpan.FromMilliseconds(300));
            slide.Update(0.5, from, to, true, 400, Array.Empty<PageTransitionItem>());

            Assert.IsType<TranslateTransform>(from.RenderTransform);
            var ft = (TranslateTransform)from.RenderTransform!;
            Assert.Equal(-200, ft.X);
        }

        [Fact]
        public void PageSlide_Update_AppliesTranslateTransformToTo()
        {
            var parent = new Canvas { Width = 400, Height = 300 };
            var from = new Border();
            var to = new Border();
            parent.Children.Add(from);
            parent.Children.Add(to);
            parent.Measure(new Size(400, 300));
            parent.Arrange(new Rect(0, 0, 400, 300));

            var slide = new PageSlide(TimeSpan.FromMilliseconds(300));
            slide.Update(0.5, from, to, true, 400, Array.Empty<PageTransitionItem>());

            Assert.IsType<TranslateTransform>(to.RenderTransform);
            var tt = (TranslateTransform)to.RenderTransform!;
            Assert.Equal(200, tt.X);
        }

        [Fact]
        public void CrossFade_Update_SetsOpacity()
        {
            var from = new Border();
            var to = new Border();

            var crossFade = new CrossFade(TimeSpan.FromMilliseconds(300));
            crossFade.Update(0.5, from, to, true, 0, Array.Empty<PageTransitionItem>());

            Assert.Equal(0.5, from.Opacity, 2);
            Assert.Equal(0.5, to.Opacity, 2);
            Assert.True(to.IsVisible);
        }

    }

    public class CarouselIsSwipeEnabledTests : ScopedTestBase
    {
        [Fact]
        public void IsSwipeEnabled_DefaultIsFalse()
        {
            var carousel = new Carousel();
            Assert.False(carousel.IsSwipeEnabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsSwipeEnabled_RoundTrips(bool value)
        {
            var carousel = new Carousel { IsSwipeEnabled = value };
            Assert.Equal(value, carousel.IsSwipeEnabled);
        }

        [Fact]
        public void GetTransitionAxis_ReturnsNull_WhenNoTransition()
        {
            var carousel = new Carousel();
            Assert.Null(carousel.GetTransitionAxis());
        }

        [Fact]
        public void GetTransitionAxis_ReturnsNull_WhenNonPageSlideTransition()
        {
            var carousel = new Carousel { PageTransition = new CrossFade(TimeSpan.FromMilliseconds(200)) };
            Assert.Null(carousel.GetTransitionAxis());
        }

        [Fact]
        public void GetTransitionAxis_ReturnsVertical_WhenPageSlideVertical()
        {
            var carousel = new Carousel
            {
                PageTransition = new PageSlide(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Vertical)
            };
            Assert.Equal(PageSlide.SlideAxis.Vertical, carousel.GetTransitionAxis());
        }

        [Fact]
        public void GetTransitionAxis_ReturnsVertical_WhenRotate3DVertical()
        {
            var carousel = new Carousel
            {
                PageTransition = new Rotate3DTransition(TimeSpan.FromMilliseconds(200), PageSlide.SlideAxis.Vertical)
            };
            Assert.Equal(PageSlide.SlideAxis.Vertical, carousel.GetTransitionAxis());
        }
    }

    private sealed class TestPageTransition : IPageTransition
    {
        public Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class FakePointer : IPointer
    {
        public int Id { get; } = Pointer.GetNextFreeId();
        public void Capture(IInputElement? control) => Captured = control;
        public IInputElement? Captured { get; set; }
        public PointerType Type => PointerType.Mouse;
        public bool IsPrimary => true;
    }

    public class VisualTreeLifecycleTests : ScopedTestBase
    {
        [Fact]
        public void Detach_And_Reattach_CollectionChangedStillUpdatesSelection()
        {
            var pages = new AvaloniaList<Page>();
            var cp = new CarouselPage { Pages = pages };
            var root = new TestRoot { Child = cp };

            var page1 = new ContentPage { Header = "A" };
            pages.Add(page1);
            Assert.Same(page1, cp.SelectedPage);

            root.Child = null;
            root.Child = cp;

            var page2 = new ContentPage { Header = "B" };
            pages.Add(page2);

            pages.Remove(page1);
            Assert.Same(page2, cp.SelectedPage);
        }
    }
}
