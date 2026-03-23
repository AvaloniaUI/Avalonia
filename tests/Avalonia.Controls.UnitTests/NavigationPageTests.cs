using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class NavigationPageTests
{
    public class PushTests : ScopedTestBase
    {
        [Fact]
        public async Task Push_SinglePage_StackDepthBecomesOne()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.Equal(1, nav.StackDepth);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public async Task Push_MultipleTimes_StackDepthMatchesCount(int n)
        {
            var nav = new NavigationPage();
            for (int i = 0; i < n; i++)
                await nav.PushAsync(new ContentPage { Header = $"Page {i}" });
            Assert.Equal(n, nav.StackDepth);
        }

        [Fact]
        public async Task Push_SetsCurrentPageToTopPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);
            Assert.Same(top, nav.CurrentPage);
        }

        [Fact]
        public async Task Push_SetsIsInNavigationPage()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            Assert.False(page.IsInNavigationPage);
            await nav.PushAsync(page);
            Assert.True(page.IsInNavigationPage);
        }

        [Fact]
        public async Task Push_SetsNavigationProperty()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            await nav.PushAsync(page);
            Assert.Same(nav, page.Navigation);
        }

        [Fact]
        public async Task Push_DuplicatePage_Throws()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            await nav.PushAsync(page);
            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushAsync(page));
        }

        [Fact]
        public async Task Push_PageAlreadyPresentedModally_Throws()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage();
            await nav.PushModalAsync(modal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushAsync(modal));
        }

        [Fact]
        public async Task Push_FiresPushedEvent()
        {
            var nav = new NavigationPage();
            NavigationEventArgs? received = null;
            nav.Pushed += (_, e) => received = e;

            var page = new ContentPage();
            await nav.PushAsync(page);

            Assert.NotNull(received);
            Assert.Same(page, received.Page);
            Assert.Equal(NavigationType.Push, received.NavigationType);
        }

        [Fact]
        public async Task Push_InvokesNavigatedTo_OnPushedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            NavigatedToEventArgs? args = null;
            var second = new ContentPage();
            second.NavigatedTo += (_, e) => args = e;

            await nav.PushAsync(second);

            Assert.NotNull(args);
            Assert.Same(root, args.PreviousPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public async Task Push_InvokesNavigatedFrom_OnPreviousPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            NavigatedFromEventArgs? args = null;
            root.NavigatedFrom += (_, e) => args = e;
            await nav.PushAsync(root);

            var second = new ContentPage();
            await nav.PushAsync(second);

            Assert.NotNull(args);
            Assert.Same(second, args.DestinationPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public async Task PushAsync_WhenNavigatingFromCancels_DoesNotPush()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            root.Navigating += args =>
            {
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PushAsync(new ContentPage());

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task Push_ReentrantFromNavigatedTo_IsIgnoredNotThrown()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var second = new ContentPage();
            second.NavigatedTo += async (_, _) => await nav.PushAsync(new ContentPage());

            await nav.PushAsync(second);

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(second, nav.CurrentPage);
        }
    }

    public class ReentrantNavigationTests : ScopedTestBase
    {
        [Fact]
        public async Task Pop_ReentrantFromNavigatedTo_IsIgnored()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            root.NavigatedTo += async (_, _) => await nav.PopAsync();

            await nav.PopAsync();

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task PushModal_ReentrantFromNavigatedTo_IsIgnored()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage();
            modal.NavigatedTo += async (_, _) => await nav.PushModalAsync(new ContentPage());

            await nav.PushModalAsync(modal);

            Assert.Equal(1, nav.ModalStack.Count);
            Assert.Same(modal, nav.ModalStack[0]);
        }

        [Fact]
        public async Task PopModal_ReentrantFromNavigatedTo_IsIgnored()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var modal = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushModalAsync(modal);

            root.NavigatedTo += async (_, _) => await nav.PopModalAsync();

            await nav.PopModalAsync();

            Assert.Equal(0, nav.ModalStack.Count);
            Assert.Same(root, nav.CurrentPage);
        }
    }

    public class PopTests : ScopedTestBase
    {
        [Fact]
        public async Task Pop_OnEmptyStack_ReturnsNull()
        {
            var nav = new NavigationPage();
            Assert.Null(await nav.PopAsync());
        }

        [Fact]
        public async Task Pop_OnRootOnly_ReturnsNull_AndKeepsRoot()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var result = await nav.PopAsync();
            Assert.Null(result);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task Pop_ReturnsPoppedPage_AndDecrementsStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            var result = await nav.PopAsync();

            Assert.Same(top, result);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task Pop_ClearsIsInNavigationPage_OnPoppedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            await nav.PopAsync();

            Assert.False(top.IsInNavigationPage);
            Assert.Null(top.Navigation);
        }

        [Fact]
        public async Task Pop_FiresPoppedEvent()
        {
            var nav = new NavigationPage();
            NavigationEventArgs? received = null;
            nav.Popped += (_, e) => received = e;

            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);
            await nav.PopAsync();

            Assert.NotNull(received);
            Assert.Same(top, received.Page);
            Assert.Equal(NavigationType.Pop, received.NavigationType);
        }

        [Fact]
        public async Task Pop_InvokesNavigatedTo_OnRevealedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            NavigatedToEventArgs? args = null;
            root.NavigatedTo += (_, e) => args = e;
            await nav.PushAsync(root);

            var top = new ContentPage();
            await nav.PushAsync(top);
            await nav.PopAsync();

            Assert.NotNull(args);
            Assert.Same(top, args.PreviousPage);
            Assert.Equal(NavigationType.Pop, args.NavigationType);
        }

        [Fact]
        public async Task PopAsync_WhenNavigatingFromCancels_DoesNotPop()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var top = new ContentPage();
            await nav.PushAsync(top);

            top.Navigating += args =>
            {
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopAsync();

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
        }

        [Fact]
        public async Task PopAsync_InvokesNavigatedTo_OnRevealedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            bool navigatedTo = false;
            root.NavigatedTo += (_, _) => navigatedTo = true;

            await nav.PopAsync();

            Assert.True(navigatedTo);
        }
    }

    public class NavigationStackTests : ScopedTestBase
    {
        [Fact]
        public async Task NavigationStack_RootAtIndexZero_TopAtLastIndex()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var middle = new ContentPage { Header = "Middle" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(middle);
            await nav.PushAsync(top);

            var stack = nav.NavigationStack;
            Assert.Equal(3, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(middle, stack[1]);
            Assert.Same(top, stack[2]);
        }

        [Fact]
        public async Task CanGoBack_FalseWithOneEntry()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.False(nav.CanGoBack);
        }

        [Fact]
        public async Task CanGoBack_TrueWithTwoEntries()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            Assert.True(nav.CanGoBack);
        }

        [Fact]
        public async Task CanGoBack_FalseAfterPop()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            await nav.PopAsync();
            Assert.False(nav.CanGoBack);
        }

        [Fact]
        public async Task StackDepth_AlwaysEqualsNavigationStack_Count()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);

            await nav.PushAsync(new ContentPage());
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);

            await nav.PopAsync();
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);
        }
    }

    public class BackButtonVisibilityTests : ScopedTestBase
    {
        [Fact]
        public async Task BackButtonVisible_FalseWhenStackDepthIsOne()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.Equal(false, nav.IsBackButtonEffectivelyVisible);
        }

        [Fact]
        public async Task BackButtonVisible_TrueWhenStackDepthIsTwo()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            Assert.Equal(true, nav.IsBackButtonEffectivelyVisible);
        }

        [Fact]
        public async Task BackButtonVisible_FalseWhenIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            Assert.Equal(false, nav.IsBackButtonEffectivelyVisible);
        }

        [Fact]
        public async Task BackButtonVisible_FalseWhenPerPageIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var top = new ContentPage();
            NavigationPage.SetHasBackButton(top, false);
            await nav.PushAsync(top);
            Assert.Equal(false, nav.IsBackButtonEffectivelyVisible);
        }

        [Fact]
        public async Task BackButtonVisible_TrueAfterRestoringGlobalVisibility()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            nav.IsBackButtonVisible = true;
            Assert.Equal(true, nav.IsBackButtonEffectivelyVisible);
        }
    }

    public class BackButtonContentTests : ScopedTestBase
    {
        [Fact]
        public async Task DrawerToggle_UsesMenuIconWithoutMutatingPageBackButtonContent()
        {
            var parts = new BackButtonParts();
            var nav = CreateNavigationPageWithBackButtonParts(parts);
            nav.Resources.Add("NavigationPageMenuIcon", new StreamGeometry());
            var drawer = new DrawerPage();
            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();
            nav.SetDrawerPage(drawer);

            var page = new ContentPage { Header = "Root" };
            await nav.PushAsync(page);

            Assert.NotNull(parts.DefaultIcon);
            Assert.NotNull(parts.ContentPresenter);
            Assert.Null(NavigationPage.GetBackButtonContent(page));
            Assert.False(parts.DefaultIcon!.IsVisible);
            Assert.IsType<PathIcon>(parts.ContentPresenter!.Content);
        }

        [Fact]
        public async Task CurrentPageBackButtonContent_UpdatesRenderedPresenter()
        {
            var parts = new BackButtonParts();
            var nav = CreateNavigationPageWithBackButtonParts(parts);
            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();

            await nav.PushAsync(new ContentPage { Header = "Root" });
            var detail = new ContentPage { Header = "Detail" };
            await nav.PushAsync(detail);

            var customContent = "Custom";
            NavigationPage.SetBackButtonContent(detail, customContent);

            Assert.NotNull(parts.DefaultIcon);
            Assert.NotNull(parts.ContentPresenter);
            Assert.False(parts.DefaultIcon!.IsVisible);
            Assert.Equal(customContent, parts.ContentPresenter!.Content);

            NavigationPage.SetBackButtonContent(detail, null);

            Assert.True(parts.DefaultIcon.IsVisible);
            Assert.Null(parts.ContentPresenter.Content);
        }

        [Fact]
        public async Task DrawerBehaviorChange_DoesNotClearCustomPathIcon()
        {
            var parts = new BackButtonParts();
            var nav = CreateNavigationPageWithBackButtonParts(parts);
            var drawer = new DrawerPage();
            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();
            nav.SetDrawerPage(drawer);

            var customIcon = new PathIcon();
            var page = new ContentPage { Header = "Root" };
            NavigationPage.SetBackButtonContent(page, customIcon);

            await nav.PushAsync(page);
            drawer.DrawerBehavior = DrawerBehavior.Locked;
            nav.SetDrawerPage(drawer);

            Assert.NotNull(parts.DefaultIcon);
            Assert.NotNull(parts.ContentPresenter);
            Assert.Same(customIcon, NavigationPage.GetBackButtonContent(page));
            Assert.Same(customIcon, parts.ContentPresenter!.Content);
            Assert.False(parts.DefaultIcon!.IsVisible);
        }

        private static NavigationPage CreateNavigationPageWithBackButtonParts(BackButtonParts parts)
        {
            return new NavigationPage
            {
                Template = new FuncControlTemplate<NavigationPage>((parent, ns) =>
                {
                    parts.DefaultIcon = new Path { Name = "PART_BackButtonDefaultIcon" }.RegisterInNameScope(ns);
                    parts.ContentPresenter = new ContentPresenter { Name = "PART_BackButtonContentPresenter" }.RegisterInNameScope(ns);

                    return new Panel
                    {
                        Children =
                        {
                            new Border
                            {
                                Name = "PART_NavigationBar",
                                Child = new Button
                                {
                                    Name = "PART_BackButton",
                                    Content = new Panel
                                    {
                                        Children =
                                        {
                                            parts.DefaultIcon,
                                            parts.ContentPresenter
                                        }
                                    }
                                }.RegisterInNameScope(ns)
                            }.RegisterInNameScope(ns),
                            new Panel
                            {
                                Name = "PART_ContentHost",
                                Children =
                                {
                                    new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
                                    new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns),
                                }
                            }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns),
                        }
                    };
                })
            };
        }

        private sealed class BackButtonParts
        {
            public Path? DefaultIcon { get; set; }

            public ContentPresenter? ContentPresenter { get; set; }
        }
    }

    public class PopToRootTests : ScopedTestBase
    {
        [Fact]
        public async Task PopToRoot_LeavesOnlyFirstPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());

            await nav.PopToRootAsync();

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task PopToRoot_FiresPoppedToRootEvent()
        {
            var nav = new NavigationPage();
            bool fired = false;
            nav.PoppedToRoot += (_, _) => fired = true;

            await nav.PushAsync(new ContentPage());
            await nav.PushAsync(new ContentPage());
            await nav.PopToRootAsync();

            Assert.True(fired);
        }

        [Fact]
        public async Task PopToRoot_InvokesNavigatedFrom_OnCurrentPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            NavigatedFromEventArgs? args = null;
            top.NavigatedFrom += (_, e) => args = e;

            await nav.PopToRootAsync();

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PopToRoot, args!.NavigationType);
            Assert.Same(root, args!.DestinationPage);
        }

        [Fact]
        public async Task PopToRoot_InvokesNavigatedTo_OnRootPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            NavigatedToEventArgs? args = null;
            root.NavigatedTo += (_, e) => args = e;

            await nav.PopToRootAsync();

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PopToRoot, args!.NavigationType);
            Assert.Same(top, args!.PreviousPage);
        }

        [Fact]
        public async Task PopToRoot_NavigatedFrom_FiresAfterStateChange()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            int stackDepthAtEvent = -1;
            top.NavigatedFrom += (_, _) => stackDepthAtEvent = nav.StackDepth;

            await nav.PopToRootAsync();

            Assert.Equal(1, stackDepthAtEvent);
        }

        [Fact]
        public async Task PopToRoot_WhenAlreadyAtRoot_DoesNothing()
        {
            var nav = new NavigationPage();
            bool fired = false;
            nav.PoppedToRoot += (_, _) => fired = true;

            await nav.PushAsync(new ContentPage());
            await nav.PopToRootAsync();

            Assert.Equal(1, nav.StackDepth);
            Assert.False(fired);
        }
    }

    public class InsertRemoveTests : ScopedTestBase
    {
        [Fact]
        public async Task InsertPage_AddsPageBeforeTarget()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            var middle = new ContentPage { Header = "Middle" };
            nav.InsertPage(middle, top);

            var stack = nav.NavigationStack;
            Assert.Equal(3, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(middle, stack[1]);
            Assert.Same(top, stack[2]);
        }

        [Fact]
        public async Task RemovePage_RemovesFromMiddleOfStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var middle = new ContentPage { Header = "Middle" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(middle);
            await nav.PushAsync(top);

            nav.RemovePage(middle);

            var stack = nav.NavigationStack;
            Assert.Equal(2, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(top, stack[1]);
        }

        [Fact]
        public async Task InsertPage_FiresPageInsertedEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            PageInsertedEventArgs? args = null;
            nav.PageInserted += (_, e) => args = e;

            var inserted = new ContentPage();
            nav.InsertPage(inserted, top);

            Assert.NotNull(args);
            Assert.Same(inserted, args.Page);
        }

        [Fact]
        public async Task RemovePage_FiresPageRemovedEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var mid = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(mid);
            await nav.PushAsync(top);

            PageRemovedEventArgs? args = null;
            nav.PageRemoved += (_, e) => args = e;

            nav.RemovePage(mid);

            Assert.NotNull(args);
            Assert.Same(mid, args.Page);
        }

        [Fact]
        public async Task InsertPage_NullPage_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            var before = new ContentPage();
            await nav.PushAsync(before);
            Assert.Throws<ArgumentNullException>(() => nav.InsertPage(null!, before));
        }

        [Fact]
        public async Task InsertPage_NullBefore_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.Throws<ArgumentNullException>(() => nav.InsertPage(new ContentPage(), null!));
        }

        [Fact]
        public async Task RemovePage_NullPage_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.Throws<ArgumentNullException>(() => nav.RemovePage(null!));
        }

        [Fact]
        public async Task InsertPage_DoesNotFireNavigatedTo_OnInsertedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            NavigatedToEventArgs? args = null;
            var inserted = new ContentPage();
            inserted.NavigatedTo += (_, e) => args = e;

            nav.InsertPage(inserted, top);

            Assert.Null(args);
        }

        [Fact]
        public async Task InsertPage_FiresNavigatedTo_WhenInsertedPageBecomesCurrent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            var inserted = new ContentPage();
            int navigatedToCount = 0;
            NavigatedToEventArgs? args = null;
            inserted.NavigatedTo += (_, e) =>
            {
                navigatedToCount++;
                args = e;
            };

            nav.InsertPage(inserted, top);
            Assert.Equal(0, navigatedToCount);

            await nav.PopAsync();

            Assert.Equal(1, navigatedToCount);
            Assert.NotNull(args);
            Assert.Equal(NavigationType.Pop, args!.NavigationType);
            Assert.Same(top, args.PreviousPage);
            Assert.Same(inserted, nav.CurrentPage);
        }

        [Fact]
        public async Task InsertPage_DuplicatePage_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            Assert.Throws<InvalidOperationException>(() => nav.InsertPage(root, top));
        }

        [Fact]
        public async Task InsertPage_PageAlreadyPresentedModally_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            var modal = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);
            await nav.PushModalAsync(modal);

            Assert.Throws<InvalidOperationException>(() => nav.InsertPage(modal, top));
        }

        [Fact]
        public async Task InsertPage_BeforeNotInStack_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var stranger = new ContentPage();
            Assert.Throws<InvalidOperationException>(() => nav.InsertPage(new ContentPage(), stranger));
        }

        [Fact]
        public async Task RemovePage_PageNotInStack_IsNoOp()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var stranger = new ContentPage();
            // Should not throw and should not change stack depth
            nav.RemovePage(stranger);
            Assert.Equal(1, nav.StackDepth);
        }

        [Fact]
        public async Task RemovePage_TopPage_FiresNavigatedFrom_WithRemoveType()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            NavigatedFromEventArgs? fromArgs = null;
            top.NavigatedFrom += (_, e) => fromArgs = e;

            nav.RemovePage(top);

            Assert.NotNull(fromArgs);
            Assert.Equal(NavigationType.Remove, fromArgs!.NavigationType);
            Assert.Same(root, fromArgs.DestinationPage);
        }

        [Fact]
        public async Task RemovePage_TopPage_FiresNavigatedTo_OnNewCurrentPage_WithRemoveType()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            NavigatedToEventArgs? toArgs = null;
            root.NavigatedTo += (_, e) => toArgs = e;

            nav.RemovePage(top);

            Assert.NotNull(toArgs);
            Assert.Equal(NavigationType.Remove, toArgs!.NavigationType);
            Assert.Same(top, toArgs.PreviousPage);
        }

        [Fact]
        public async Task RemovePage_TopPage_UpdatesCurrentPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            nav.RemovePage(top);

            Assert.Same(root, nav.CurrentPage);
            Assert.Equal(1, nav.StackDepth);
        }
    }

    public class ModalTests : ScopedTestBase
    {
        [Fact]
        public async Task PushModal_AddsToModalStack()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            Assert.Equal(1, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PushModal_PageAlreadyInNavigationStack_Throws()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushModalAsync(root));
        }

        [Fact]
        public async Task PushModal_DuplicateModal_Throws()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.PushModalAsync(modal));
        }

        [Fact]
        public async Task PopModal_RemovesFromModalStack()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage();
            await nav.PushModalAsync(modal);
            await nav.PopModalAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task ModalStack_IsOrderedBottomToTop()
        {
            // Index 0 = oldest (bottom-most); last index = topmost, consistent with NavigationStack.
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            var m3 = new ContentPage { Header = "M3" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);
            await nav.PushModalAsync(m3);

            Assert.Equal(new[] { m1, m2, m3 }, nav.ModalStack);
        }

        [Fact]
        public async Task PushModal_FiresModalPushedEvent()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            ModalPushedEventArgs? args = null;
            nav.ModalPushed += (_, e) => args = e;

            var modal = new ContentPage();
            await nav.PushModalAsync(modal);

            Assert.NotNull(args);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public async Task PopModal_FiresModalPoppedEvent()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage();
            await nav.PushModalAsync(modal);

            ModalPoppedEventArgs? args = null;
            nav.ModalPopped += (_, e) => args = e;

            await nav.PopModalAsync();

            Assert.NotNull(args);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public async Task PopModal_OnEmptyStack_ReturnsNull()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var result = await nav.PopModalAsync();
            Assert.Null(result);
        }

        [Fact]
        public async Task PushModal_InvokesNavigatedFrom_OnCoveredPage_WithPushModalType()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            NavigatedFromEventArgs? args = null;
            root.NavigatedFrom += (_, e) => args = e;

            await nav.PushModalAsync(new ContentPage { Header = "Modal" });

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PushModal, args!.NavigationType);
        }

        [Fact]
        public async Task PushModal_InvokesNavigatedTo_OnModalPage_WithPushModalType()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var modal = new ContentPage { Header = "Modal" };
            NavigatedToEventArgs? args = null;
            modal.NavigatedTo += (_, e) => args = e;

            await nav.PushModalAsync(modal);

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PushModal, args!.NavigationType);
        }

        [Fact]
        public async Task PopModal_InvokesNavigatedFrom_OnPoppedModal_WithPopModalType()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            NavigatedFromEventArgs? args = null;
            modal.NavigatedFrom += (_, e) => args = e;

            await nav.PopModalAsync();

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PopModal, args!.NavigationType);
        }

        [Fact]
        public async Task PopModal_InvokesNavigatedTo_OnRevealedPage_WithPopModalType()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);
            await nav.PushModalAsync(new ContentPage { Header = "Modal" });

            NavigatedToEventArgs? args = null;
            root.NavigatedTo += (_, e) => args = e;

            await nav.PopModalAsync();

            Assert.NotNull(args);
            Assert.Equal(NavigationType.PopModal, args!.NavigationType);
        }

        [Fact]
        public async Task PushModal_NavigatedFrom_DestinationPage_IsTheModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            var modal = new ContentPage { Header = "Modal" };
            NavigatedFromEventArgs? args = null;
            root.NavigatedFrom += (_, e) => args = e;

            await nav.PushModalAsync(modal);

            Assert.NotNull(args);
            Assert.Same(modal, args!.DestinationPage);
        }

        [Fact]
        public async Task PushModal_NavigatedTo_PreviousPage_IsCoveredPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            var modal = new ContentPage { Header = "Modal" };
            NavigatedToEventArgs? args = null;
            modal.NavigatedTo += (_, e) => args = e;

            await nav.PushModalAsync(modal);

            Assert.NotNull(args);
            Assert.Same(root, args!.PreviousPage);
        }

        [Fact]
        public async Task PopModal_NavigatedFrom_DestinationPage_IsRevealedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var modal = new ContentPage { Header = "Modal" };
            await nav.PushAsync(root);
            await nav.PushModalAsync(modal);

            NavigatedFromEventArgs? args = null;
            modal.NavigatedFrom += (_, e) => args = e;

            await nav.PopModalAsync();

            Assert.NotNull(args);
            Assert.Same(root, args!.DestinationPage);
        }

        [Fact]
        public async Task PopModal_NavigatedTo_PreviousPage_IsPoppedModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var modal = new ContentPage { Header = "Modal" };
            await nav.PushAsync(root);
            await nav.PushModalAsync(modal);

            NavigatedToEventArgs? args = null;
            root.NavigatedTo += (_, e) => args = e;

            await nav.PopModalAsync();

            Assert.NotNull(args);
            Assert.Same(modal, args!.PreviousPage);
        }

        [Fact]
        public async Task PushModal_WhenCoveredPageCancelsNavigating_DoesNotPushModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            bool handlerInvoked = false;
            root.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PushModalAsync(new ContentPage { Header = "Modal" });

            Assert.True(handlerInvoked);
            Assert.Empty(nav.ModalStack);
        }

        [Fact]
        public async Task PushModal_WhenTopModalCancelsNavigating_DoesNotPushAnotherModal()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage { Header = "Root" });

            var firstModal = new ContentPage { Header = "Modal 1" };
            await nav.PushModalAsync(firstModal);

            bool handlerInvoked = false;
            firstModal.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PushModalAsync(new ContentPage { Header = "Modal 2" });

            Assert.True(handlerInvoked);
            Assert.Single(nav.ModalStack);
            Assert.Same(firstModal, nav.ModalStack[0]);
        }

        [Fact]
        public async Task PopModal_WhenModalCancelsNavigating_DoesNotPopModal()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage { Header = "Root" });

            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            bool handlerInvoked = false;
            modal.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            var result = await nav.PopModalAsync();

            Assert.True(handlerInvoked);
            Assert.Null(result);
            Assert.Single(nav.ModalStack);
            Assert.Same(modal, nav.ModalStack[0]);
        }
    }

    public class SystemBackButtonTests : ScopedTestBase
    {
        private static RoutedEventArgs RaiseBackButton(NavigationPage nav)
        {
            var args = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
            nav.RaiseEvent(args);
            return args;
        }

        [Fact]
        public async Task BackButton_WithModalOnRoot_PopsModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var modal = new ContentPage { Header = "Modal" };
            await nav.PushAsync(root);
            await nav.PushModalAsync(modal);

            var args = RaiseBackButton(nav);

            Assert.True(args.Handled);
            Assert.Empty(nav.ModalStack);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public async Task BackButton_WithModalAndDeepStack_PopsModalBeforeStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var detail = new ContentPage { Header = "Detail" };
            var modal = new ContentPage { Header = "Modal" };
            await nav.PushAsync(root);
            await nav.PushAsync(detail);
            await nav.PushModalAsync(modal);

            var args = RaiseBackButton(nav);

            Assert.True(args.Handled);
            Assert.Empty(nav.ModalStack);
            Assert.Equal(2, nav.StackDepth);
            Assert.Same(detail, nav.CurrentPage);
        }

        [Fact]
        public async Task BackButton_ForwardsToTopModalBeforeAutoPop()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage { Header = "Root" });

            var modal = new BackHandlingPage { HandleBack = true };
            await nav.PushModalAsync(modal);

            var args = RaiseBackButton(nav);

            Assert.True(args.Handled);
            Assert.Equal(1, modal.BackButtonPressCount);
            Assert.Single(nav.ModalStack);
            Assert.Same(modal, nav.ModalStack[0]);
        }

        private sealed class BackHandlingPage : ContentPage
        {
            public int BackButtonPressCount { get; private set; }

            public bool HandleBack { get; set; }

            protected override bool OnSystemBackButtonPressed()
            {
                BackButtonPressCount++;
                return HandleBack;
            }
        }
    }

    public class PropertyTests : ScopedTestBase
    {
        [Fact]
        public void IsGestureEnabled_Default_IsTrue()
        {
            var nav = new NavigationPage();
            Assert.True(nav.IsGestureEnabled);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void IsGestureEnabled_RoundTrips(bool value)
        {
            var nav = new NavigationPage { IsGestureEnabled = value };
            Assert.Equal(value, nav.IsGestureEnabled);
        }
    }

    public class AttachedPropertyTests : ScopedTestBase
    {
        [Fact]
        public async Task EffectiveBarHeight_UsesPageOverride()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            NavigationPage.SetBarHeightOverride(page, 60.0);
            await nav.PushAsync(page);
            Assert.Equal(60.0, nav.EffectiveBarHeight);
        }

        [Fact]
        public async Task EffectiveBarHeight_FallsBackToGlobalBarHeight()
        {
            var nav = new NavigationPage { BarHeight = 56.0 };
            await nav.PushAsync(new ContentPage());
            Assert.Equal(56.0, nav.EffectiveBarHeight);
        }

        [Fact]
        public async Task BarLayoutBehavior_DefaultAppliesNavBarInsetPseudoClass()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            Assert.True(nav.Classes.Contains(":nav-bar-inset"));
        }

        [Fact]
        public async Task BarLayoutBehavior_OverlayRemovesNavBarInsetPseudoClass()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
            await nav.PushAsync(page);
            Assert.False(nav.Classes.Contains(":nav-bar-inset"));
        }
    }

    public class InitialContentTests : ScopedTestBase
    {
        [Fact]
        public void Content_SetBeforePush_IsUsedAsInitialPage()
        {
            var page = new ContentPage { Header = "Initial" };
            var nav = new NavigationPage { Content = page };
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(page, nav.CurrentPage);
        }

        [Fact]
        public async Task Content_SetAfterPush_IsIgnored()
        {
            var nav = new NavigationPage();
            var first = new ContentPage { Header = "First" };
            await nav.PushAsync(first);

            // Setting Content when stack is already populated should not push again
            nav.Content = new ContentPage { Header = "Second" };
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(first, nav.CurrentPage);
        }
    }

    public class TransitionCancellationTests : ScopedTestBase
    {
        [Fact]
        public async Task PushAsync_WithTransition_WhenCancelled_StackUnchangedAfterCancel()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            // Cancel the first navigation
            bool shouldCancel = true;
            root.Navigating += args =>
            {
                if (shouldCancel)
                    args.Cancel = true;
                return Task.CompletedTask;
            };

            var customTransition = new CrossFade(TimeSpan.FromMilliseconds(100));
            await nav.PushAsync(new ContentPage(), customTransition);

            Assert.Equal(1, nav.StackDepth);

            // Stop cancelling and push again: override should not leak
            shouldCancel = false;
            var second = new ContentPage();
            await nav.PushAsync(second);
            Assert.Equal(2, nav.StackDepth);
            Assert.Same(second, nav.CurrentPage);
        }

        [Fact]
        public async Task PopAsync_WithTransition_WhenCancelled_StackUnchangedAfterCancel()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            top.Navigating += args =>
            {
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopAsync(new CrossFade(TimeSpan.FromMilliseconds(100)));

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
        }
    }

    public class NavigatingEventTests : ScopedTestBase
    {
        [Fact]
        public async Task PopToRootAsync_AwaitsAsyncNavigatingHandler()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            bool handlerInvoked = false;
            top.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopToRootAsync();

            Assert.True(handlerInvoked);
            Assert.Equal(2, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
        }

        [Fact]
        public async Task PopToPageAsync_AwaitsAsyncNavigatingHandler()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var middle = new ContentPage();
            await nav.PushAsync(middle);
            var top = new ContentPage();
            await nav.PushAsync(top);

            bool handlerInvoked = false;
            top.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopToPageAsync(root);

            Assert.True(handlerInvoked);
            Assert.Equal(3, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
        }

        [Fact]
        public async Task Push_Sync_InvokesNavigatingEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            bool handlerInvoked = false;
            root.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PushAsync(new ContentPage());

            Assert.True(handlerInvoked);
            Assert.Equal(1, nav.StackDepth);
        }

        [Fact]
        public async Task Pop_Sync_InvokesNavigatingEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            bool handlerInvoked = false;
            top.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopAsync();

            Assert.True(handlerInvoked);
            Assert.Equal(2, nav.StackDepth);
        }

        [Fact]
        public async Task Navigating_CancelInFirstHandler_SkipsSubsequentHandlers()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            bool secondHandlerInvoked = false;
            top.Navigating += args => { args.Cancel = true; return Task.CompletedTask; };
            top.Navigating += args => { secondHandlerInvoked = true; return Task.CompletedTask; };

            await nav.PopAsync();

            Assert.False(secondHandlerInvoked);
            Assert.Equal(2, nav.StackDepth);
        }

        [Fact]
        public async Task Navigating_CancelInOnNavigatingFrom_SkipsNavigatingEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var top = new CancellingPage();
            await nav.PushAsync(top);

            bool asyncHandlerInvoked = false;
            top.Navigating += args => { asyncHandlerInvoked = true; return Task.CompletedTask; };

            await nav.PopAsync();

            Assert.False(asyncHandlerInvoked);
            Assert.Equal(2, nav.StackDepth);
        }

        private sealed class CancellingPage : ContentPage
        {
            protected override void OnNavigatingFrom(NavigatingFromEventArgs args) => args.Cancel = true;
        }
    }

    public class LogicalChildrenTests : ScopedTestBase
    {
        [Fact]
        public async Task Pop_RemovesPageFromLogicalChildren()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var top = new ContentPage();
            await nav.PushAsync(top);

            await nav.PopAsync();

            Assert.DoesNotContain(top, nav.GetLogicalChildren());
            Assert.Contains(root, nav.GetLogicalChildren());
        }

        [Fact]
        public async Task PopToRootAsync_RemovesIntermediatePagesFromLogicalChildren()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            var middle = new ContentPage();
            await nav.PushAsync(middle);
            var top = new ContentPage();
            await nav.PushAsync(top);

            await nav.PopToRootAsync();

            Assert.Contains(root, nav.GetLogicalChildren());
            Assert.DoesNotContain(middle, nav.GetLogicalChildren());
            Assert.DoesNotContain(top, nav.GetLogicalChildren());
        }

        [Fact]
        public async Task ReplaceAsync_SwapsLogicalChildren()
        {
            var nav = new NavigationPage();
            var original = new ContentPage();
            await nav.PushAsync(original);

            var replacement = new ContentPage();
            await nav.ReplaceAsync(replacement);

            Assert.DoesNotContain(original, nav.GetLogicalChildren());
            Assert.Contains(replacement, nav.GetLogicalChildren());
        }

        [Fact]
        public async Task PushAsync_AddsPageToLogicalChildren()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var second = new ContentPage();
            await nav.PushAsync(second);

            var children = new List<ILogical>(nav.GetLogicalChildren());
            Assert.Contains(root, children);
            Assert.Contains(second, children);
        }

        [Fact]
        public async Task PushModal_ModalPageIsNotInDirectLogicalChildren()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var modal = new ContentPage();
            await nav.PushModalAsync(modal);

            Assert.DoesNotContain(modal, nav.GetLogicalChildren());
            Assert.Same(nav, modal.Navigation);
            Assert.True(modal.IsInNavigationPage);
        }

        [Fact]
        public async Task PopModal_ClearsNavigationAndIsInNavigationPage()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var modal = new ContentPage();
            await nav.PushModalAsync(modal);

            await nav.PopModalAsync();

            Assert.Null(modal.Navigation);
            Assert.False(modal.IsInNavigationPage);
        }
    }

    public class PopAllModalsTests : ScopedTestBase
    {
        [Fact]
        public async Task PopAllModals_EmptyStack_DoesNothing()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            await nav.PopAllModalsAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopAllModals_SingleModal_ClearsModalStack()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await nav.PushModalAsync(new ContentPage());

            await nav.PopAllModalsAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopAllModals_MultipleModals_ClearsEntireStack()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await nav.PushModalAsync(new ContentPage { Header = "M1" });
            await nav.PushModalAsync(new ContentPage { Header = "M2" });
            await nav.PushModalAsync(new ContentPage { Header = "M3" });

            await nav.PopAllModalsAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopAllModals_FiresModalPopped_ForEachModal()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            var m3 = new ContentPage { Header = "M3" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);
            await nav.PushModalAsync(m3);

            var popped = new List<Page>();
            nav.ModalPopped += (_, e) => popped.Add(e.Modal);

            await nav.PopAllModalsAsync();

            Assert.Equal(3, popped.Count);
            // LIFO order: m3 was pushed last so must be popped first.
            Assert.Equal(new[] { m3, m2, m1 }, popped);
        }

        [Fact]
        public async Task PopAllModals_FiresNavigatedFrom_OnAllModals()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            var navigatedFrom = new List<string>();
            m1.NavigatedFrom += (_, _) => navigatedFrom.Add("M1");
            m2.NavigatedFrom += (_, _) => navigatedFrom.Add("M2");

            await nav.PopAllModalsAsync();

            // LIFO order: M2 was pushed last, so it navigates from first.
            Assert.Equal(new[] { "M2", "M1" }, navigatedFrom);
        }

        [Fact]
        public async Task PopAllModals_FiresNavigatedTo_OnUnderlyingPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);
            await nav.PushModalAsync(new ContentPage { Header = "M1" });
            await nav.PushModalAsync(new ContentPage { Header = "M2" });

            bool navigatedTo = false;
            root.NavigatedTo  += (_, _) => navigatedTo = true;

            await nav.PopAllModalsAsync();

            Assert.True(navigatedTo);
        }

        [Fact]
        public async Task PopAllModals_NavigatedFrom_NavigationTypeIsPopModal()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            var m1 = new ContentPage();
            var m2 = new ContentPage();
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            NavigationType? m1Type = null;
            NavigationType? m2Type = null;
            m1.NavigatedFrom += (_, e) => m1Type = e.NavigationType;
            m2.NavigatedFrom += (_, e) => m2Type = e.NavigationType;

            await nav.PopAllModalsAsync();

            Assert.Equal(NavigationType.PopModal, m1Type);
            Assert.Equal(NavigationType.PopModal, m2Type);
        }

        [Fact]
        public async Task PopAllModals_NavigatedTo_NavigationTypeIsPopModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushModalAsync(new ContentPage());

            NavigationType? type = null;
            root.NavigatedTo += (_, e) => type = e.NavigationType;

            await nav.PopAllModalsAsync();

            Assert.Equal(NavigationType.PopModal, type);
        }

        [Fact]
        public async Task PopAllModals_NavigatedTo_PreviousPageIsTopModal()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var m1 = new ContentPage();
            var m2 = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            Page? previousPage = null;
            root.NavigatedTo += (_, e) => previousPage = e.PreviousPage;

            await nav.PopAllModalsAsync();

            Assert.Same(m2, previousPage);
        }

        [Fact]
        public async Task PopAllModals_MultipleModals_NavigatedTo_FiredOnlyOnBaseCurrentPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            var navigatedToPages = new List<string>();
            root.NavigatedTo += (_, _) => navigatedToPages.Add("root");
            m1.NavigatedTo += (_, _) => navigatedToPages.Add("m1");
            m2.NavigatedTo += (_, _) => navigatedToPages.Add("m2");

            await nav.PopAllModalsAsync();

            Assert.Equal(["root"], navigatedToPages);
        }

        [Fact]
        public async Task PopAllModals_WhenTopModalCancelsNavigating_DoesNotClearModalStack()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage { Header = "Root" });

            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            bool handlerInvoked = false;
            m2.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            await nav.PopAllModalsAsync();

            Assert.True(handlerInvoked);
            Assert.Equal(new[] { m1, m2 }, nav.ModalStack);
        }
    }

    public class ReplaceTests : ScopedTestBase
    {
        [Fact]
        public async Task ReplaceAsync_NullPage_Throws()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());
            await Assert.ThrowsAsync<ArgumentNullException>(() => nav.ReplaceAsync(null!));
        }

        [Fact]
        public async Task ReplaceAsync_OnEmptyStack_PushesPage()
        {
            var nav = new NavigationPage();
            var page = new ContentPage { Header = "Replaced" };

            await nav.ReplaceAsync(page);

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(page, nav.CurrentPage);
        }

        [Fact]
        public async Task ReplaceAsync_ReplacesTopPage_StackDepthUnchanged()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            var replacement = new ContentPage { Header = "Replacement" };
            await nav.ReplaceAsync(replacement);

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(replacement, nav.CurrentPage);
            Assert.DoesNotContain(root, nav.NavigationStack);
        }

        [Fact]
        public async Task ReplaceAsync_WhenReplacementAlreadyExistsInNavigationStack_Throws()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.ReplaceAsync(root));
        }

        [Fact]
        public async Task ReplaceAsync_WhenReplacementAlreadyExistsInModalStack_Throws()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage { Header = "Root" });
            await nav.PushAsync(new ContentPage { Header = "Top" });

            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            await Assert.ThrowsAsync<InvalidOperationException>(() => nav.ReplaceAsync(modal));
        }

        [Fact]
        public async Task ReplaceAsync_WithCurrentPage_IsNoOp()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            var lifecycleEvents = 0;
            top.NavigatedFrom += (_, _) => lifecycleEvents++;
            top.NavigatedTo += (_, _) => lifecycleEvents++;

            await nav.ReplaceAsync(top);

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
            Assert.Equal(0, lifecycleEvents);
        }

        [Fact]
        public async Task ReplaceAsync_FiresLifecycleEventsInOrder()
        {
            var nav = new NavigationPage();
            var original = new ContentPage { Header = "Original" };
            await nav.PushAsync(original);

            var replacement = new ContentPage { Header = "Replacement" };
            var order = new List<string>();
            original.NavigatedFrom  += (_, _) => order.Add("Original: NavigatedFrom");
            replacement.NavigatedTo += (_, _) => order.Add("Replacement: NavigatedTo");

            await nav.ReplaceAsync(replacement);

            Assert.Equal(new[]
            {
                "Original: NavigatedFrom",
                "Replacement: NavigatedTo",
            }, order);
        }

        [Fact]
        public async Task ReplaceAsync_NavigationType_IsReplace()
        {
            var nav = new NavigationPage();
            var original = new ContentPage { Header = "Original" };
            await nav.PushAsync(original);

            var replacement = new ContentPage { Header = "Replacement" };
            NavigationType? arrivedType   = null;
            NavigationType? departedType  = null;
            replacement.NavigatedTo  += (_, e) => arrivedType  = e.NavigationType;
            original.NavigatedFrom   += (_, e) => departedType = e.NavigationType;

            await nav.ReplaceAsync(replacement);

            Assert.Equal(NavigationType.Replace, arrivedType);
            Assert.Equal(NavigationType.Replace, departedType);
        }

        [Fact]
        public async Task ReplaceAsync_WhenCancelled_DoesNotReplace()
        {
            var nav = new NavigationPage();
            var original = new ContentPage { Header = "Original" };
            await nav.PushAsync(original);

            original.Navigating += args =>
            {
                args.Cancel = true;
                return Task.CompletedTask;
            };

            var replacement = new ContentPage { Header = "Replacement" };
            await nav.ReplaceAsync(replacement);

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(original, nav.CurrentPage);
        }
    }

    public class PopToRootLifecycleTests : ScopedTestBase
    {
        [Fact]
        public async Task PopToRootAsync_PreviousPage_IsNotNull()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var second = new ContentPage { Header = "Second" };
            var third = new ContentPage { Header = "Third" };
            await nav.PushAsync(root);
            await nav.PushAsync(second);
            await nav.PushAsync(third);

            Page? receivedPreviousPage = null;
            root.NavigatedTo += (_, args) => receivedPreviousPage = args.PreviousPage;

            await nav.PopToRootAsync();

            Assert.Same(third, receivedPreviousPage);
        }

        [Fact]
        public async Task PopToRootAsync_NavigationType_IsPopToRoot()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);
            await nav.PushAsync(new ContentPage());

            NavigationType? receivedType = null;
            root.NavigatedTo += (_, args) => receivedType = args.NavigationType;

            await nav.PopToRootAsync();

            Assert.Equal(NavigationType.PopToRoot, receivedType);
        }
    }

    public class PopToPageLifecycleTests : ScopedTestBase
    {
        [Fact]
        public async Task PopToPageAsync_PreviousPage_IsNotNull()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var target = new ContentPage { Header = "Target" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(target);
            await nav.PushAsync(top);

            Page? receivedPreviousPage = null;
            target.NavigatedTo += (_, args) => receivedPreviousPage = args.PreviousPage;

            await nav.PopToPageAsync(target);

            Assert.Same(top, receivedPreviousPage);
        }

        [Fact]
        public async Task PopToPageAsync_NavigationType_IsPop()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var target = new ContentPage { Header = "Target" };
            await nav.PushAsync(root);
            await nav.PushAsync(target);
            await nav.PushAsync(new ContentPage());

            NavigationType? receivedType = null;
            target.NavigatedTo += (_, args) => receivedType = args.NavigationType;

            await nav.PopToPageAsync(target);

            Assert.Equal(NavigationType.Pop, receivedType);
        }

        [Fact]
        public async Task PopToPageAsync_IntermediatePages_NavigationTypeIsPop()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var target = new ContentPage();
            var middle = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(target);
            await nav.PushAsync(middle);
            await nav.PushAsync(top);

            NavigationType? middleType = null;
            NavigationType? topType = null;
            middle.NavigatedFrom += (_, e) => middleType = e.NavigationType;
            top.NavigatedFrom += (_, e) => topType = e.NavigationType;

            await nav.PopToPageAsync(target);

            Assert.Equal(NavigationType.Pop, topType);
            Assert.Equal(NavigationType.Pop, middleType);
        }

        [Fact]
        public async Task PopToPageAsync_PageNotInStack_ThrowsArgumentException()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var stranger = new ContentPage();
            await Assert.ThrowsAsync<ArgumentException>(() => nav.PopToPageAsync(stranger));
        }

        [Fact]
        public async Task PopToPageAsync_TargetIsAlreadyTop_IsNoOp()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            int navigatedToCount = 0;
            top.NavigatedTo += (_, _) => navigatedToCount++;

            await nav.PopToPageAsync(top);

            Assert.Same(top, nav.CurrentPage);
            Assert.Equal(2, nav.StackDepth);
            Assert.Equal(0, navigatedToCount);
        }
    }

    public class ModalTransitionCancellationTests : ScopedTestBase
    {
        [Fact]
        public async Task PushModalAsync_CancelledTransition_StillFiresLifecycleEvents()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var modal = new ContentPage();
            bool navigatedFromFired = false;
            bool navigatedToFired = false;
            bool modalPushedFired = false;
            root.NavigatedFrom += (_, _) => navigatedFromFired = true;
            modal.NavigatedTo += (_, _) => navigatedToFired = true;
            nav.ModalPushed += (_, _) => modalPushedFired = true;

            await nav.PushModalAsync(modal, null);

            Assert.True(navigatedFromFired);
            Assert.True(navigatedToFired);
            Assert.True(modalPushedFired);
            Assert.Equal(1, nav.ModalStack.Count);
            Assert.Same(modal, nav.ModalStack[0]);
        }

        [Fact]
        public async Task PopAllModalsAsync_CancelledTransition_StillClearsStackAndFiresEvents()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var m1 = new ContentPage();
            var m2 = new ContentPage();
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            bool m1PoppedFired = false;
            bool m2PoppedFired = false;
            bool rootNavigatedToFired = false;
            nav.ModalPopped += (_, e) =>
            {
                if (ReferenceEquals(e.Modal, m1)) m1PoppedFired = true;
                if (ReferenceEquals(e.Modal, m2)) m2PoppedFired = true;
            };
            root.NavigatedTo += (_, _) => rootNavigatedToFired = true;

            await nav.PopAllModalsAsync(null);

            Assert.Equal(0, nav.ModalStack.Count);
            Assert.True(m1PoppedFired);
            Assert.True(m2PoppedFired);
            Assert.True(rootNavigatedToFired);
        }
    }

    public class LifecycleAfterTransitionTests : ScopedTestBase
    {
        [Fact]
        public async Task PushAsync_LifecycleEvents_FireAfterTransition()
        {
            var tcs = new TaskCompletionSource();
            var transition = new ControllableTransition(tcs.Task);
            var nav = CreateNavigationPage(transition);

            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            bool navigatedFromDuringTransition = false;
            bool navigatedToDuringTransition = false;
            bool pushedDuringTransition = false;

            var second = new ContentPage { Header = "Second" };
            root.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
            second.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
            nav.Pushed += (_, _) => pushedDuringTransition = !tcs.Task.IsCompleted;

            var pushTask = nav.PushAsync(second);

            tcs.SetResult();
            await pushTask;

            Assert.False(navigatedFromDuringTransition);
            Assert.False(navigatedToDuringTransition);
            Assert.False(pushedDuringTransition);
        }

        [Fact]
        public async Task PopAsync_LifecycleEvents_FireAfterTransition()
        {
            var tcs = new TaskCompletionSource();
            var nav = CreateNavigationPage(null);

            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            nav.PageTransition = new ControllableTransition(tcs.Task);

            bool navigatedFromDuringTransition = false;
            bool navigatedToDuringTransition = false;
            bool poppedDuringTransition = false;

            top.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
            root.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
            nav.Popped += (_, _) => poppedDuringTransition = !tcs.Task.IsCompleted;

            var popTask = nav.PopAsync();

            tcs.SetResult();
            await popTask;

            Assert.False(navigatedFromDuringTransition);
            Assert.False(navigatedToDuringTransition);
            Assert.False(poppedDuringTransition);
        }

        [Fact]
        public async Task PopToRootAsync_LifecycleEvents_FireAfterTransition()
        {
            var tcs = new TaskCompletionSource();
            var nav = CreateNavigationPage(null);

            var root = new ContentPage { Header = "Root" };
            var second = new ContentPage { Header = "Second" };
            var third = new ContentPage { Header = "Third" };
            await nav.PushAsync(root);
            await nav.PushAsync(second);
            await nav.PushAsync(third);

            nav.PageTransition = new ControllableTransition(tcs.Task);

            bool navigatedFromDuringTransition = false;
            bool navigatedToDuringTransition = false;
            bool poppedToRootDuringTransition = false;

            second.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
            third.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
            root.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;
            nav.PoppedToRoot += (_, _) => poppedToRootDuringTransition = !tcs.Task.IsCompleted;

            var popTask = nav.PopToRootAsync();

            tcs.SetResult();
            await popTask;

            Assert.False(navigatedFromDuringTransition);
            Assert.False(navigatedToDuringTransition);
            Assert.False(poppedToRootDuringTransition);
        }

        [Fact]
        public async Task ReplaceAsync_LifecycleEvents_FireAfterTransition()
        {
            var tcs = new TaskCompletionSource();
            var nav = CreateNavigationPage(null);

            var root = new ContentPage { Header = "Root" };
            await nav.PushAsync(root);

            nav.PageTransition = new ControllableTransition(tcs.Task);

            bool navigatedFromDuringTransition = false;
            bool navigatedToDuringTransition = false;

            var replacement = new ContentPage { Header = "Replacement" };
            root.NavigatedFrom += (_, _) => navigatedFromDuringTransition = !tcs.Task.IsCompleted;
            replacement.NavigatedTo += (_, _) => navigatedToDuringTransition = !tcs.Task.IsCompleted;

            var replaceTask = nav.ReplaceAsync(replacement);

            tcs.SetResult();
            await replaceTask;

            Assert.False(navigatedFromDuringTransition);
            Assert.False(navigatedToDuringTransition);
        }

        private static NavigationPage CreateNavigationPage(IPageTransition? transition)
        {
            var nav = new NavigationPage
            {
                PageTransition = transition,
                Template = NavigationPageTemplate()
            };
            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();
            return nav;
        }

        private static IControlTemplate NavigationPageTemplate()
        {
            return new FuncControlTemplate<NavigationPage>((parent, ns) =>
            {
                var contentHost = new Panel
                {
                    Name = "PART_ContentHost",
                    Children =
                    {
                        new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
                        new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns),
                    }
                }.RegisterInNameScope(ns);

                return new Panel
                {
                    Children =
                    {
                        new Border
                        {
                            Name = "PART_NavigationBar",
                            Child = new Button { Name = "PART_BackButton" }.RegisterInNameScope(ns)
                        }.RegisterInNameScope(ns),
                        contentHost,
                        new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
                        new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
                        new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns),
                    }
                };
            });
        }

        private class ControllableTransition : IPageTransition
        {
            private readonly Task _gate;

            public ControllableTransition(Task gate)
            {
                _gate = gate;
            }

            public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
            {
                if (to != null)
                    to.IsVisible = true;
                await _gate;
                if (from != null)
                    from.IsVisible = false;
            }
        }
    }

    public class SwipeGestureTests : ScopedTestBase
    {
        [Fact]
        public async Task HandledPointerPressedAtEdge_AllowsSwipePop()
        {
            var nav = new NavigationPage();
            var rootPage = new ContentPage { Header = "Root" };
            var topPage = new ContentPage { Header = "Top" };

            await nav.PushAsync(rootPage);
            await nav.PushAsync(topPage);

            var root = new TestRoot { Child = nav };
            root.ExecuteInitialLayoutPass();

            RaiseHandledPointerPressed(nav, new Point(5, 5));

            var swipe = new SwipeGestureEventArgs(1, new Vector(-20, 0), default);
            nav.RaiseEvent(swipe);
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.True(swipe.Handled);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(rootPage, nav.CurrentPage);
        }

        [Fact]
        public async Task MouseEdgeDrag_AllowsSwipePop()
        {
            var nav = new NavigationPage
            {
                Width = 400,
                Height = 300
            };
            nav.GestureRecognizers.OfType<SwipeGestureRecognizer>().First().IsMouseEnabled = true;
            var rootPage = new ContentPage { Header = "Root" };
            var topPage = new ContentPage { Header = "Top" };

            await nav.PushAsync(rootPage);
            await nav.PushAsync(topPage);

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = nav
            };
            root.ExecuteInitialLayoutPass();

            var mouse = new MouseTestHelper();
            mouse.Down(nav, position: new Point(5, 5));
            mouse.Move(nav, new Point(40, 5));
            mouse.Up(nav, position: new Point(40, 5));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(rootPage, nav.CurrentPage);
        }

        [Fact]
        public async Task SameGestureId_OnlyPops_One_Page()
        {
            var nav = new NavigationPage
            {
                Width = 400,
                Height = 300
            };
            var page1 = new ContentPage { Header = "1" };
            var page2 = new ContentPage { Header = "2" };
            var page3 = new ContentPage { Header = "3" };

            await nav.PushAsync(page1);
            await nav.PushAsync(page2);
            await nav.PushAsync(page3);

            var root = new TestRoot
            {
                ClientSize = new Size(400, 300),
                Child = nav
            };
            root.ExecuteInitialLayoutPass();

            RaiseHandledPointerPressed(nav, new Point(5, 5));

            nav.RaiseEvent(new SwipeGestureEventArgs(42, new Vector(-20, 0), default));
            nav.RaiseEvent(new SwipeGestureEventArgs(42, new Vector(-30, 0), default));
            Dispatcher.UIThread.RunJobs(null, TestContext.Current.CancellationToken);

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(page2, nav.CurrentPage);
        }

        private static void RaiseHandledPointerPressed(Interactive target, Point position)
        {
            var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
            var args = new PointerPressedEventArgs(
                target,
                pointer,
                (Visual)target,
                position,
                timestamp: 1,
                new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                KeyModifiers.None)
            {
                Handled = true
            };

            target.RaiseEvent(args);
        }
    }

    public class IsNavigatingTests : ScopedTestBase
    {
        [Fact]
        public void IsNavigating_FalseByDefault()
        {
            var nav = new NavigationPage();
            Assert.False(nav.IsNavigating);
        }

        [Fact]
        public async Task IsNavigating_TrueWhilePushInProgress()
        {
            var tcs = new TaskCompletionSource<bool>();
            var nav = CreateNavigationPage(new ControllableTransition(tcs.Task));

            await nav.PushAsync(new ContentPage());

            bool duringPush = false;
            var pushTask = nav.PushAsync(new ContentPage());
            duringPush = nav.IsNavigating;
            tcs.SetResult(true);
            await pushTask;

            Assert.True(duringPush);
        }

        [Fact]
        public async Task IsNavigating_FalseAfterPushCompletes()
        {
            var tcs = new TaskCompletionSource<bool>();
            var nav = CreateNavigationPage(new ControllableTransition(tcs.Task));

            await nav.PushAsync(new ContentPage());
            var pushTask = nav.PushAsync(new ContentPage());
            tcs.SetResult(true);
            await pushTask;

            Assert.False(nav.IsNavigating);
        }

        private static NavigationPage CreateNavigationPage(IPageTransition? transition)
        {
            var nav = new NavigationPage
            {
                PageTransition = transition,
                Template = new FuncControlTemplate<NavigationPage>((parent, ns) =>
                {
                    return new Panel
                    {
                        Children =
                        {
                            new Panel
                            {
                                Name = "PART_ContentHost",
                                Children =
                                {
                                    new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
                                    new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns),
                                }
                            }.RegisterInNameScope(ns),
                            new Border { Name = "PART_NavigationBar",
                                Child = new Button { Name = "PART_BackButton" }.RegisterInNameScope(ns) }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns),
                        }
                    };
                })
            };
            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();
            return nav;
        }

        private class ControllableTransition : IPageTransition
        {
            private readonly Task _gate;

            public ControllableTransition(Task gate)
            {
                _gate = gate;
            }

            public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
            {
                if (to != null)
                    to.IsVisible = true;
                await _gate;
                if (from != null)
                    from.IsVisible = false;
            }
        }
    }

    public class VisualTreeLifecycleTests : ScopedTestBase
    {
        [Fact]
        public async Task Detach_And_Reattach_PreservesModalStack()
        {
            var nav = new NavigationPage
            {
                Template = new FuncControlTemplate<NavigationPage>((parent, ns) =>
                {
                    return new Panel
                    {
                        Children =
                        {
                            new Panel
                            {
                                Name = "PART_ContentHost",
                                Children =
                                {
                                    new ContentPresenter { Name = "PART_PageBackPresenter" }.RegisterInNameScope(ns),
                                    new ContentPresenter { Name = "PART_PagePresenter" }.RegisterInNameScope(ns),
                                }
                            }.RegisterInNameScope(ns),
                            new Border
                            {
                                Name = "PART_NavigationBar",
                                Child = new Button { Name = "PART_BackButton" }.RegisterInNameScope(ns)
                            }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_TopCommandBar" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalBackPresenter" }.RegisterInNameScope(ns),
                            new ContentPresenter { Name = "PART_ModalPresenter" }.RegisterInNameScope(ns),
                        }
                    };
                })
            };

            var root = new TestRoot { Child = nav };
            root.LayoutManager.ExecuteInitialLayoutPass();

            var page = new ContentPage { Header = "Root" };
            var modal = new ContentPage { Header = "Modal" };

            await nav.PushAsync(page);
            await nav.PushModalAsync(modal);

            root.Child = null;

            Assert.Single(nav.ModalStack);
            Assert.Same(modal, nav.ModalStack[0]);
            Assert.Null(modal.Navigation);
            Assert.False(modal.IsInNavigationPage);

            root.Child = nav;
            root.LayoutManager.ExecuteInitialLayoutPass();

            Assert.Single(nav.ModalStack);
            Assert.Same(modal, nav.ModalStack[0]);
            Assert.Same(nav, modal.Navigation);
            Assert.True(modal.IsInNavigationPage);
        }
    }

    public class PagesPropertyTests : ScopedTestBase
    {
        [Fact]
        public void Pages_DirectAssignment_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var originalPages = nav.Pages;

            Assert.Throws<InvalidOperationException>(() => nav.Pages = new List<Page> { new ContentPage() });

            Assert.Same(originalPages, nav.Pages);
            Assert.Empty(nav.NavigationStack);
            Assert.Null(nav.CurrentPage);
        }

        [Fact]
        public void Pages_AssignNull_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var originalPages = nav.Pages;

            Assert.Throws<InvalidOperationException>(() => nav.Pages = null);

            Assert.Same(originalPages, nav.Pages);
            Assert.Empty(nav.NavigationStack);
            Assert.Null(nav.CurrentPage);
        }

        [Fact]
        public async Task Pages_DirectAssignment_DoesNotCorruptExistingStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);
            var originalPages = nav.Pages;

            Assert.Throws<InvalidOperationException>(() => nav.Pages = new List<Page> { new ContentPage() });

            Assert.Same(originalPages, nav.Pages);
            Assert.Equal(2, nav.NavigationStack.Count);
            Assert.Same(root, nav.NavigationStack[0]);
            Assert.Same(top, nav.NavigationStack[1]);
            Assert.Same(top, nav.CurrentPage);
        }
    }

    public class ContentPageCoerceTests : ScopedTestBase
    {
        [Fact]
        public void ContentPage_ContentSetToPage_ThrowsInvalidOperationException()
        {
            var page = new ContentPage();
            Assert.Throws<InvalidOperationException>(() => page.Content = new ContentPage());
        }

        [Fact]
        public void ContentPage_ContentSetToNonPage_DoesNotThrow()
        {
            var page = new ContentPage();
            page.Content = "hello";
            Assert.Equal("hello", page.Content);
        }
    }

    public class DrawerPageFirstPageTests : ScopedTestBase
    {
        [Fact]
        public void DrawerPage_ContentReplaced_ResendsNavigatedToOnLoad()
        {
            var root = new TestRoot();
            var nav1 = new NavigationPage();
            var page1 = new ContentPage();
            var drawer = new DrawerPage { Content = nav1 };
            root.Child = drawer;
            root.LayoutManager.ExecuteInitialLayoutPass();

            NavigatedToEventArgs? firstArgs = null;
            page1.NavigatedTo += (_, e) => firstArgs = e;
            nav1.Content = page1;

            Assert.NotNull(firstArgs);

            var nav2 = new NavigationPage();
            var page2 = new ContentPage();

            NavigatedToEventArgs? secondArgs = null;
            page2.NavigatedTo += (_, e) => secondArgs = e;

            drawer.Content = nav2;
            nav2.Content = page2;

            Assert.NotNull(secondArgs);
        }
    }

}
