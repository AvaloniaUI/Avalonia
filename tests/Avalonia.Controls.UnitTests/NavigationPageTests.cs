using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.UnitTests;
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
            // Verifies that a re-entrant Push called from inside a NavigatedTo
            // lifecycle callback is silently ignored rather than throwing.
            var nav = new NavigationPage();
            var root = new ContentPage();
            await nav.PushAsync(root);

            var second = new ContentPage();
            second.NavigatedTo += async (_, _) => await nav.PushAsync(new ContentPage());

            await nav.PushAsync(second); // must not throw

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(second, nav.CurrentPage);
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
            // InsertPage should not invoke NavigatedTo on a non-current (background) page.
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            await nav.PushAsync(root);
            await nav.PushAsync(top);

            bool navigatedToFired = false;
            var inserted = new ContentPage();
            inserted.NavigatedTo += (_, _) => navigatedToFired = true;

            nav.InsertPage(inserted, top);

            Assert.False(navigatedToFired);
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
        public async Task RemovePage_PageNotInStack_IsNoOp()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            var stranger = new ContentPage();
            // Should not throw and should not change stack depth
            nav.RemovePage(stranger);
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
    }

    public class PopAllModalsTests : ScopedTestBase
    {
        [Fact]
        public async Task PopAllModals_EmptyStack_DoesNothing()
        {
            var nav = new NavigationPage();
            await nav.PushAsync(new ContentPage());

            await nav.PopAllModalsAsync(); // must not throw

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

}
