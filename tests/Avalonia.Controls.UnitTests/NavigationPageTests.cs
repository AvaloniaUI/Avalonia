using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class NavigationPageTests
{
    public class PushTests : ScopedTestBase
    {
        [Fact]
        public void Push_SinglePage_StackDepthBecomesOne()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.Equal(1, nav.StackDepth);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(10)]
        public void Push_MultipleTimes_StackDepthMatchesCount(int n)
        {
            var nav = new NavigationPage();
            for (int i = 0; i < n; i++)
                nav.Push(new ContentPage { Header = $"Page {i}" });
            Assert.Equal(n, nav.StackDepth);
        }

        [Fact]
        public void Push_SetsCurrentPageToTopPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            nav.Push(root);
            nav.Push(top);
            Assert.Same(top, nav.CurrentPage);
        }

        [Fact]
        public void Push_SetsIsInNavigationPage()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            Assert.False(page.IsInNavigationPage);
            nav.Push(page);
            Assert.True(page.IsInNavigationPage);
        }

        [Fact]
        public void Push_SetsNavigationProperty()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            nav.Push(page);
            Assert.Same(nav, page.Navigation);
        }

        [Fact]
        public void Push_DuplicatePage_Throws()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            nav.Push(page);
            Assert.Throws<InvalidOperationException>(() => nav.Push(page));
        }

        [Fact]
        public void Push_FiresPushedEvent()
        {
            var nav = new NavigationPage();
            NavigationEventArgs? received = null;
            nav.Pushed += (_, e) => received = e;

            var page = new ContentPage();
            nav.Push(page);

            Assert.NotNull(received);
            Assert.Same(page, received.Page);
            Assert.Equal(NavigationType.Push, received.NavigationType);
        }

        [Fact]
        public void Push_InvokesNavigatedTo_OnPushedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);

            NavigatedToEventArgs? args = null;
            var second = new ContentPage();
            second.NavigatedTo += (_, e) => args = e;

            nav.Push(second);

            Assert.NotNull(args);
            Assert.Same(root, args.PreviousPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public void Push_InvokesNavigatedFrom_OnPreviousPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            NavigatedFromEventArgs? args = null;
            root.NavigatedFrom += (_, e) => args = e;
            nav.Push(root);

            var second = new ContentPage();
            nav.Push(second);

            Assert.NotNull(args);
            Assert.Same(second, args.DestinationPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public async Task PushAsync_WhenNavigatingFromCancels_DoesNotPush()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);

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
        public void Push_ReentrantFromNavigatedTo_IsIgnoredNotThrown()
        {
            // Verifies that a re-entrant Push called from inside a NavigatedTo
            // lifecycle callback is silently ignored rather than throwing.
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);

            var second = new ContentPage();
            second.NavigatedTo += (_, _) => nav.Push(new ContentPage());

            nav.Push(second); // must not throw

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(second, nav.CurrentPage);
        }
    }

    public class PopTests : ScopedTestBase
    {
        [Fact]
        public void Pop_OnEmptyStack_ReturnsNull()
        {
            var nav = new NavigationPage();
            Assert.Null(nav.Pop());
        }

        [Fact]
        public void Pop_OnRootOnly_ReturnsNull_AndKeepsRoot()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            var result = nav.Pop();
            Assert.Null(result);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public void Pop_ReturnsPoppedPage_AndDecrementsStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);

            var result = nav.Pop();

            Assert.Same(top, result);
            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public void Pop_ClearsIsInNavigationPage_OnPoppedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);

            nav.Pop();

            Assert.False(top.IsInNavigationPage);
            Assert.Null(top.Navigation);
        }

        [Fact]
        public void Pop_FiresPoppedEvent()
        {
            var nav = new NavigationPage();
            NavigationEventArgs? received = null;
            nav.Popped += (_, e) => received = e;

            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);
            nav.Pop();

            Assert.NotNull(received);
            Assert.Same(top, received.Page);
            Assert.Equal(NavigationType.Pop, received.NavigationType);
        }

        [Fact]
        public void Pop_InvokesNavigatedTo_OnRevealedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            NavigatedToEventArgs? args = null;
            root.NavigatedTo += (_, e) => args = e;
            nav.Push(root);

            var top = new ContentPage();
            nav.Push(top);
            nav.Pop();

            Assert.NotNull(args);
            Assert.Same(top, args.PreviousPage);
            Assert.Equal(NavigationType.Pop, args.NavigationType);
        }

        [Fact]
        public async Task PopAsync_WhenNavigatingFromCancels_DoesNotPop()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var top = new ContentPage();
            nav.Push(top);

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
        public async Task PopAsync_InvokesAppearing_AndNavigatedTo_OnRevealedPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            var top = new ContentPage();
            nav.Push(top);

            bool appearing = false;
            bool navigatedTo = false;
            root.Appearing   += (_, _) => appearing   = true;
            root.NavigatedTo += (_, _) => navigatedTo = true;

            await nav.PopAsync();

            Assert.True(appearing);
            Assert.True(navigatedTo);
        }
    }

    public class NavigationStackTests : ScopedTestBase
    {
        [Fact]
        public void NavigationStack_RootAtIndexZero_TopAtLastIndex()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var middle = new ContentPage { Header = "Middle" };
            var top = new ContentPage { Header = "Top" };
            nav.Push(root);
            nav.Push(middle);
            nav.Push(top);

            var stack = nav.NavigationStack;
            Assert.Equal(3, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(middle, stack[1]);
            Assert.Same(top, stack[2]);
        }

        [Fact]
        public void CanGoBack_FalseWithOneEntry()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.False(nav.CanGoBack);
        }

        [Fact]
        public void CanGoBack_TrueWithTwoEntries()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            Assert.True(nav.CanGoBack);
        }

        [Fact]
        public void CanGoBack_FalseAfterPop()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.Pop();
            Assert.False(nav.CanGoBack);
        }

        [Fact]
        public void StackDepth_AlwaysEqualsNavigationStack_Count()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);

            nav.Push(new ContentPage());
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);

            nav.Pop();
            Assert.Equal(nav.NavigationStack.Count, nav.StackDepth);
        }
    }

    public class BackButtonVisibilityTests : ScopedTestBase
    {
        [Fact]
        public void BackButtonVisible_FalseWhenStackDepthIsOne()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.Equal(false, nav.BackButtonVisibleEffective);
        }

        [Fact]
        public void BackButtonVisible_TrueWhenStackDepthIsTwo()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            Assert.Equal(true, nav.BackButtonVisibleEffective);
        }

        [Fact]
        public void BackButtonVisible_FalseWhenIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            Assert.Equal(false, nav.BackButtonVisibleEffective);
        }

        [Fact]
        public void BackButtonVisible_FalseWhenPerPageIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var top = new ContentPage();
            NavigationPage.SetHasBackButton(top, false);
            nav.Push(top);
            Assert.Equal(false, nav.BackButtonVisibleEffective);
        }

        [Fact]
        public void BackButtonVisible_TrueAfterRestoringGlobalVisibility()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.IsBackButtonVisible = true;
            Assert.Equal(true, nav.BackButtonVisibleEffective);
        }
    }

    public class PopToRootTests : ScopedTestBase
    {
        [Fact]
        public async Task PopToRoot_LeavesOnlyFirstPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            nav.Push(root);
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());

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

            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            await nav.PopToRootAsync();

            Assert.True(fired);
        }

        [Fact]
        public async Task PopToRoot_WhenAlreadyAtRoot_DoesNothing()
        {
            var nav = new NavigationPage();
            bool fired = false;
            nav.PoppedToRoot += (_, _) => fired = true;

            nav.Push(new ContentPage());
            await nav.PopToRootAsync();

            Assert.Equal(1, nav.StackDepth);
            Assert.False(fired);
        }
    }

    public class InsertRemoveTests : ScopedTestBase
    {
        [Fact]
        public void InsertPage_AddsPageBeforeTarget()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var top = new ContentPage { Header = "Top" };
            nav.Push(root);
            nav.Push(top);

            var middle = new ContentPage { Header = "Middle" };
            nav.InsertPage(middle, top);

            var stack = nav.NavigationStack;
            Assert.Equal(3, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(middle, stack[1]);
            Assert.Same(top, stack[2]);
        }

        [Fact]
        public void RemovePage_RemovesFromMiddleOfStack()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            var middle = new ContentPage { Header = "Middle" };
            var top = new ContentPage { Header = "Top" };
            nav.Push(root);
            nav.Push(middle);
            nav.Push(top);

            nav.RemovePage(middle);

            var stack = nav.NavigationStack;
            Assert.Equal(2, stack.Count);
            Assert.Same(root, stack[0]);
            Assert.Same(top, stack[1]);
        }

        [Fact]
        public void InsertPage_FiresPageInsertedEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);

            PageInsertedEventArgs? args = null;
            nav.PageInserted += (_, e) => args = e;

            var inserted = new ContentPage();
            nav.InsertPage(inserted, top);

            Assert.NotNull(args);
            Assert.Same(inserted, args.Page);
        }

        [Fact]
        public void RemovePage_FiresPageRemovedEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var mid = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(mid);
            nav.Push(top);

            PageRemovedEventArgs? args = null;
            nav.PageRemoved += (_, e) => args = e;

            nav.RemovePage(mid);

            Assert.NotNull(args);
            Assert.Same(mid, args.Page);
        }

        [Fact]
        public void InsertPage_NullPage_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            var before = new ContentPage();
            nav.Push(before);
            Assert.Throws<ArgumentNullException>(() => nav.InsertPage(null!, before));
        }

        [Fact]
        public void InsertPage_NullBefore_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.Throws<ArgumentNullException>(() => nav.InsertPage(new ContentPage(), null!));
        }

        [Fact]
        public void RemovePage_NullPage_ThrowsArgumentNullException()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.Throws<ArgumentNullException>(() => nav.RemovePage(null!));
        }

        [Fact]
        public void InsertPage_DoesNotFireNavigatedTo_OnInsertedPage()
        {
            // InsertPage should not invoke NavigatedTo on a non-current (background) page.
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);

            bool navigatedToFired = false;
            var inserted = new ContentPage();
            inserted.NavigatedTo += (_, _) => navigatedToFired = true;

            nav.InsertPage(inserted, top);

            Assert.False(navigatedToFired);
        }

        [Fact]
        public void InsertPage_DuplicatePage_ThrowsInvalidOperationException()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            var top = new ContentPage();
            nav.Push(root);
            nav.Push(top);

            Assert.Throws<InvalidOperationException>(() => nav.InsertPage(root, top));
        }

        [Fact]
        public void RemovePage_PageNotInStack_IsNoOp()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

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
            nav.Push(new ContentPage());

            var modal = new ContentPage { Header = "Modal" };
            await nav.PushModalAsync(modal);

            Assert.Equal(1, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopModal_RemovesFromModalStack()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            var modal = new ContentPage();
            await nav.PushModalAsync(modal);
            await nav.PopModalAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task ModalStack_EnumeratesLIFO()
        {
            // Doc: "The top (most recently pushed) modal is enumerated first."
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            var m3 = new ContentPage { Header = "M3" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);
            await nav.PushModalAsync(m3);

            Assert.Equal(new[] { m3, m2, m1 }, nav.ModalStack);
        }

        [Fact]
        public async Task PushModal_FiresModalPushedEvent()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

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
            nav.Push(new ContentPage());

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
            nav.Push(new ContentPage());

            var result = await nav.PopModalAsync();
            Assert.Null(result);
        }
    }

    public class AttachedPropertyTests : ScopedTestBase
    {
        [Fact]
        public void SetHasNavigationBar_False_AffectsPage()
        {
            var page = new ContentPage();
            NavigationPage.SetHasNavigationBar(page, false);
            Assert.False(NavigationPage.GetHasNavigationBar(page));
        }

        [Fact]
        public void SetBarHeight_Override_ReturnsOverride()
        {
            var page = new ContentPage();
            NavigationPage.SetBarHeight(page, 64.0);
            Assert.Equal(64.0, NavigationPage.GetBarHeight(page));
        }

        [Fact]
        public void BarHeightEffective_UsesPageOverride()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            NavigationPage.SetBarHeight(page, 60.0);
            nav.Push(page);
            Assert.Equal(60.0, nav.BarHeightEffective);
        }

        [Fact]
        public void BarHeightEffective_FallsBackToGlobalBarHeight()
        {
            var nav = new NavigationPage { BarHeight = 56.0 };
            nav.Push(new ContentPage());
            Assert.Equal(56.0, nav.BarHeightEffective);
        }

        [Fact]
        public void SetBarLayoutBehavior_Overlay_AffectsPage()
        {
            var page = new ContentPage();
            NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
            Assert.Equal(BarLayoutBehavior.Overlay, NavigationPage.GetBarLayoutBehavior(page));
        }

        [Fact]
        public void BarLayoutBehaviorEffective_DefaultIsInset()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.Equal(BarLayoutBehavior.Inset, nav.BarLayoutBehaviorEffective);
        }

        [Fact]
        public void BarLayoutBehaviorEffective_UsesPageOverride()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            NavigationPage.SetBarLayoutBehavior(page, BarLayoutBehavior.Overlay);
            nav.Push(page);
            Assert.Equal(BarLayoutBehavior.Overlay, nav.BarLayoutBehaviorEffective);
        }

        [Fact]
        public void SetBackButtonContent_RoundTrips()
        {
            var page = new ContentPage();
            var content = new TextBlock { Text = "Back" };
            NavigationPage.SetBackButtonContent(page, content);
            Assert.Same(content, NavigationPage.GetBackButtonContent(page));
        }

        [Fact]
        public void SetBackButtonContent_Null_ClearsValue()
        {
            var page = new ContentPage();
            NavigationPage.SetBackButtonContent(page, new TextBlock());
            NavigationPage.SetBackButtonContent(page, null);
            Assert.Null(NavigationPage.GetBackButtonContent(page));
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
        public void Content_SetAfterPush_IsIgnored()
        {
            var nav = new NavigationPage();
            var first = new ContentPage { Header = "First" };
            nav.Push(first);

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
            nav.Push(root);

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
            nav.Push(root);
            var top = new ContentPage();
            nav.Push(top);

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
            nav.Push(root);
            var top = new ContentPage();
            nav.Push(top);

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
            nav.Push(root);
            var middle = new ContentPage();
            nav.Push(middle);
            var top = new ContentPage();
            nav.Push(top);

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
        public void Push_Sync_InvokesNavigatingEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);

            bool handlerInvoked = false;
            root.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            nav.Push(new ContentPage());

            Assert.True(handlerInvoked);
            Assert.Equal(1, nav.StackDepth);
        }

        [Fact]
        public void Pop_Sync_InvokesNavigatingEvent()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            var top = new ContentPage();
            nav.Push(top);

            bool handlerInvoked = false;
            top.Navigating += args =>
            {
                handlerInvoked = true;
                args.Cancel = true;
                return Task.CompletedTask;
            };

            nav.Pop();

            Assert.True(handlerInvoked);
            Assert.Equal(2, nav.StackDepth);
        }
    }

    public class LogicalChildrenTests : ScopedTestBase
    {
        [Fact]
        public void Push_AddsPageToLogicalChildren()
        {
            var nav = new NavigationPage();
            var page = new ContentPage();
            nav.Push(page);

            Assert.Contains(page, nav.GetLogicalChildren());
        }

        [Fact]
        public void Pop_RemovesPageFromLogicalChildren()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            var top = new ContentPage();
            nav.Push(top);

            nav.Pop();

            Assert.DoesNotContain(top, nav.GetLogicalChildren());
            Assert.Contains(root, nav.GetLogicalChildren());
        }

        [Fact]
        public async Task PopToRootAsync_RemovesIntermediatePagesFromLogicalChildren()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);
            var middle = new ContentPage();
            nav.Push(middle);
            var top = new ContentPage();
            nav.Push(top);

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
            nav.Push(original);

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
            nav.Push(root);

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
            nav.Push(new ContentPage());

            await nav.PopAllModalsAsync(); // must not throw

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopAllModals_SingleModal_ClearsModalStack()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            await nav.PushModalAsync(new ContentPage());

            await nav.PopAllModalsAsync();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public async Task PopAllModals_MultipleModals_ClearsEntireStack()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
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
            nav.Push(new ContentPage());
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
        public async Task PopAllModals_FiresDisappearing_OnAllModals()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var m1 = new ContentPage { Header = "M1" };
            var m2 = new ContentPage { Header = "M2" };
            await nav.PushModalAsync(m1);
            await nav.PushModalAsync(m2);

            var disappeared = new List<string>();
            m1.Disappearing += (_, _) => disappeared.Add("M1");
            m2.Disappearing += (_, _) => disappeared.Add("M2");

            await nav.PopAllModalsAsync();

            // LIFO order: M2 was pushed last, so it disappears first.
            Assert.Equal(new[] { "M2", "M1" }, disappeared);
        }

        [Fact]
        public async Task PopAllModals_FiresAppearingAndNavigatedTo_OnUnderlyingPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            nav.Push(root);
            await nav.PushModalAsync(new ContentPage { Header = "M1" });
            await nav.PushModalAsync(new ContentPage { Header = "M2" });

            bool appeared = false;
            bool navigatedTo = false;
            root.Appearing    += (_, _) => appeared    = true;
            root.NavigatedTo  += (_, _) => navigatedTo = true;

            await nav.PopAllModalsAsync();

            Assert.True(appeared);
            Assert.True(navigatedTo);
        }
    }

    public class ReplaceTests : ScopedTestBase
    {
        [Fact]
        public async Task ReplaceAsync_NullPage_Throws()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
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
            nav.Push(root);

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
            nav.Push(original);

            var replacement = new ContentPage { Header = "Replacement" };
            var order = new List<string>();
            original.Disappearing   += (_, _) => order.Add("Original: Disappearing");
            original.NavigatedFrom  += (_, _) => order.Add("Original: NavigatedFrom");
            replacement.NavigatedTo += (_, _) => order.Add("Replacement: NavigatedTo");
            replacement.Appearing   += (_, _) => order.Add("Replacement: Appearing");

            await nav.ReplaceAsync(replacement);

            Assert.Equal(new[]
            {
                "Original: Disappearing",
                "Original: NavigatedFrom",
                "Replacement: NavigatedTo",
                "Replacement: Appearing",
            }, order);
        }

        [Fact]
        public async Task ReplaceAsync_NavigationType_IsReplace()
        {
            var nav = new NavigationPage();
            var original = new ContentPage { Header = "Original" };
            nav.Push(original);

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
            nav.Push(original);

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

    public class IsBackButtonEnabledTests : ScopedTestBase
    {
        [Fact]
        public void SetIsBackButtonEnabled_False_GetReturnsFalse()
        {
            var page = new ContentPage();
            NavigationPage.SetIsBackButtonEnabled(page, false);
            Assert.False(NavigationPage.GetIsBackButtonEnabled(page));
        }

        [Fact]
        public void SetIsBackButtonEnabled_RestoreTrue_GetReturnsTrue()
        {
            var page = new ContentPage();
            NavigationPage.SetIsBackButtonEnabled(page, false);
            NavigationPage.SetIsBackButtonEnabled(page, true);
            Assert.True(NavigationPage.GetIsBackButtonEnabled(page));
        }

        [Fact]
        public void SetIsBackButtonEnabled_False_DoesNotAffectOtherPages()
        {
            var page1 = new ContentPage();
            var page2 = new ContentPage();
            NavigationPage.SetIsBackButtonEnabled(page1, false);

            Assert.False(NavigationPage.GetIsBackButtonEnabled(page1));
            Assert.True(NavigationPage.GetIsBackButtonEnabled(page2));
        }
    }

    public class CommandBarAttachedPropertyTests : ScopedTestBase
    {
        [Fact]
        public void SetTopCommandBar_RoundTrips()
        {
            var page = new ContentPage();
            var bar = new CommandBar();
            NavigationPage.SetTopCommandBar(page, bar);
            Assert.Same(bar, NavigationPage.GetTopCommandBar(page));
        }

        [Fact]
        public void SetBottomCommandBar_RoundTrips()
        {
            var page = new ContentPage();
            var bar = new CommandBar();
            NavigationPage.SetBottomCommandBar(page, bar);
            Assert.Same(bar, NavigationPage.GetBottomCommandBar(page));
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
            nav.Push(root);
            nav.Push(second);
            nav.Push(third);

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
            nav.Push(root);
            nav.Push(new ContentPage());

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
            nav.Push(root);
            nav.Push(target);
            nav.Push(top);

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
            nav.Push(root);
            nav.Push(target);
            nav.Push(new ContentPage());

            NavigationType? receivedType = null;
            target.NavigatedTo += (_, args) => receivedType = args.NavigationType;

            await nav.PopToPageAsync(target);

            Assert.Equal(NavigationType.Pop, receivedType);
        }
    }
}
