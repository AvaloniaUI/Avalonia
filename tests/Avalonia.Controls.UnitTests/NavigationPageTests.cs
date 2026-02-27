using System;
using Avalonia.Animation;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class NavigationPageTests
{
    public class PropertyDefaults : ScopedTestBase
    {
        [Fact]
        public void BarHeight_DefaultIs48()
        {
            var nav = new NavigationPage();
            Assert.Equal(48.0, nav.BarHeight);
        }

        [Fact]
        public void HasShadow_DefaultIsFalse()
        {
            var nav = new NavigationPage();
            Assert.False(nav.HasShadow);
        }

        [Fact]
        public void IsBackButtonVisible_DefaultIsTrue()
        {
            var nav = new NavigationPage();
            Assert.True(nav.IsBackButtonVisible);
        }

        [Fact]
        public void IsGestureEnabled_DefaultIsTrue()
        {
            var nav = new NavigationPage();
            Assert.True(nav.IsGestureEnabled);
        }

        [Fact]
        public void BarBackground_DefaultIsNull()
        {
            var nav = new NavigationPage();
            Assert.Null(nav.BarBackground);
        }

        [Fact]
        public void BarForeground_DefaultIsNull()
        {
            var nav = new NavigationPage();
            Assert.Null(nav.BarForeground);
        }

        [Fact]
        public void StackDepth_InitiallyZero()
        {
            var nav = new NavigationPage();
            Assert.Equal(0, nav.StackDepth);
        }

        [Fact]
        public void CurrentPage_InitiallyNull()
        {
            var nav = new NavigationPage();
            Assert.Null(nav.CurrentPage);
        }

        [Fact]
        public void CanGoBack_InitiallyFalse()
        {
            var nav = new NavigationPage();
            Assert.False(nav.CanGoBack);
        }

        [Fact]
        public void HasNavigationBar_DefaultIsTrue()
        {
            var page = new ContentPage();
            Assert.True(NavigationPage.GetHasNavigationBar(page));
        }

        [Fact]
        public void HasBackButton_DefaultIsTrue()
        {
            var page = new ContentPage();
            Assert.True(NavigationPage.GetHasBackButton(page));
        }
    }

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
        public void Push_NPages_StackDepthEqualsN(int n)
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
        public void PushAsync_WhenNavigatingFromCancels_DoesNotPush()
        {
            var nav = new NavigationPage();
            var root = new ContentPage();
            nav.Push(root);

            root.Navigating += args =>
            {
                args.Cancel = true;
                return System.Threading.Tasks.Task.CompletedTask;
            };

            nav.PushAsync(new ContentPage()).GetAwaiter().GetResult();

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
        public void PopAsync_WhenNavigatingFromCancels_DoesNotPop()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var top = new ContentPage();
            nav.Push(top);

            top.Navigating += args =>
            {
                args.Cancel = true;
                return System.Threading.Tasks.Task.CompletedTask;
            };

            nav.PopAsync().GetAwaiter().GetResult();

            Assert.Equal(2, nav.StackDepth);
            Assert.Same(top, nav.CurrentPage);
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
    }

    public class BackButtonVisibilityTests : ScopedTestBase
    {
        [Fact]
        public void BackButtonVisible_FalseWhenStackDepthIsOne()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            Assert.False(nav.BackButtonVisibleEffective ?? false);
        }

        [Fact]
        public void BackButtonVisible_TrueWhenStackDepthIsTwo()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            Assert.True(nav.BackButtonVisibleEffective ?? false);
        }

        [Fact]
        public void BackButtonVisible_FalseWhenIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            Assert.False(nav.BackButtonVisibleEffective ?? false);
        }

        [Fact]
        public void BackButtonVisible_FalseWhenPerPageIsBackButtonVisibleIsFalse()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());
            var top = new ContentPage();
            NavigationPage.SetHasBackButton(top, false);
            nav.Push(top);
            Assert.False(nav.BackButtonVisibleEffective ?? false);
        }

        [Fact]
        public void BackButtonVisible_TrueAfterRestoringGlobalVisibility()
        {
            var nav = new NavigationPage { IsBackButtonVisible = false };
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.IsBackButtonVisible = true;
            Assert.True(nav.BackButtonVisibleEffective ?? false);
        }
    }

    public class PopToRootTests : ScopedTestBase
    {
        [Fact]
        public void PopToRoot_LeavesOnlyFirstPage()
        {
            var nav = new NavigationPage();
            var root = new ContentPage { Header = "Root" };
            nav.Push(root);
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.Push(new ContentPage());

            nav.PopToRootAsync().GetAwaiter().GetResult();

            Assert.Equal(1, nav.StackDepth);
            Assert.Same(root, nav.CurrentPage);
        }

        [Fact]
        public void PopToRoot_FiresPoppedToRootEvent()
        {
            var nav = new NavigationPage();
            bool fired = false;
            nav.PoppedToRoot += (_, _) => fired = true;

            nav.Push(new ContentPage());
            nav.Push(new ContentPage());
            nav.PopToRootAsync().GetAwaiter().GetResult();

            Assert.True(fired);
        }

        [Fact]
        public void PopToRoot_WhenAlreadyAtRoot_DoesNothing()
        {
            var nav = new NavigationPage();
            bool fired = false;
            nav.PoppedToRoot += (_, _) => fired = true;

            nav.Push(new ContentPage());
            nav.PopToRootAsync().GetAwaiter().GetResult();

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
    }

    public class ModalTests : ScopedTestBase
    {
        [Fact]
        public void PushModal_AddsToModalStack()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            var modal = new ContentPage { Header = "Modal" };
            nav.PushModalAsync(modal).GetAwaiter().GetResult();

            Assert.Equal(1, nav.ModalStack.Count);
        }

        [Fact]
        public void PopModal_RemovesFromModalStack()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            var modal = new ContentPage();
            nav.PushModalAsync(modal).GetAwaiter().GetResult();
            nav.PopModalAsync().GetAwaiter().GetResult();

            Assert.Equal(0, nav.ModalStack.Count);
        }

        [Fact]
        public void PushModal_FiresModalPushedEvent()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            ModalPushedEventArgs? args = null;
            nav.ModalPushed += (_, e) => args = e;

            var modal = new ContentPage();
            nav.PushModalAsync(modal).GetAwaiter().GetResult();

            Assert.NotNull(args);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public void PopModal_FiresModalPoppedEvent()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            var modal = new ContentPage();
            nav.PushModalAsync(modal).GetAwaiter().GetResult();

            ModalPoppedEventArgs? args = null;
            nav.ModalPopped += (_, e) => args = e;

            nav.PopModalAsync().GetAwaiter().GetResult();

            Assert.NotNull(args);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public void PopModal_OnEmptyStack_ReturnsNull()
        {
            var nav = new NavigationPage();
            nav.Push(new ContentPage());

            var result = nav.PopModalAsync().GetAwaiter().GetResult();
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
    }

    public class ContentPropertyTests : ScopedTestBase
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
}
