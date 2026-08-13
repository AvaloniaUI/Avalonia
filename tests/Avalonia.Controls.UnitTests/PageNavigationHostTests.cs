using Avalonia.Collections;
using Avalonia.Interactivity;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class PageNavigationHostTests
{
    public class LifecycleEventTests : ScopedTestBase
    {
        [Fact]
        public void Page_Set_FiresNavigatedTo()
        {
            var page = new ContentPage { Header = "Home" };
            var fired = false;
            page.NavigatedTo += (_, _) => fired = true;

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = page;

            Assert.True(fired);
        }

        [Fact]
        public void Page_Set_NavigatedTo_NavigationTypeIsReplace()
        {
            var page = new ContentPage { Header = "Home" };
            NavigatedToEventArgs? args = null;
            page.NavigatedTo += (_, e) => args = e;

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = page;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }

        [Fact]
        public void Page_Set_NavigatedTo_PreviousPageIsNull()
        {
            var page = new ContentPage { Header = "Home" };
            NavigatedToEventArgs? args = null;
            page.NavigatedTo += (_, e) => args = e;

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = page;

            Assert.NotNull(args);
            Assert.Null(args!.PreviousPage);
        }

        [Fact]
        public void Page_Changed_FiresNavigatedFrom_OnOldPage()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = first;

            NavigatedFromEventArgs? args = null;
            first.NavigatedFrom += (_, e) => args = e;

            host.Page = second;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
            Assert.Same(second, args!.DestinationPage);
        }

        [Fact]
        public void Page_Changed_FiresNavigatedTo_OnNewPage()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = first;

            NavigatedToEventArgs? args = null;
            second.NavigatedTo += (_, e) => args = e;

            host.Page = second;

            Assert.NotNull(args);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
            Assert.Same(first, args!.PreviousPage);
        }

        [Fact]
        public void Page_SetToNull_FiresNavigatedFrom_OnOldPage()
        {
            var page = new ContentPage { Header = "Home" };
            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = page;

            NavigatedFromEventArgs? args = null;
            page.NavigatedFrom += (_, e) => args = e;

            host.Page = null;

            Assert.NotNull(args);
            Assert.Null(args!.DestinationPage);
            Assert.Equal(NavigationType.Replace, args!.NavigationType);
        }

        [Fact]
        public void Page_Changed_FiresLifecycleEventsInOrder()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };

            var host = new PageNavigationHost();
            var root = new TestRoot { Child = host };
            host.Page = first;

            var order = new System.Collections.Generic.List<string>();
            first.NavigatedFrom += (_, _) => order.Add("NavigatedFrom");
            second.NavigatedTo  += (_, _) => order.Add("NavigatedTo");

            host.Page = second;

            Assert.Equal(2, order.Count);
            Assert.Equal("NavigatedFrom", order[0]);
            Assert.Equal("NavigatedTo",   order[1]);
        }

        [Fact]
        public void InitialLayout_WithExistingPage_DoesNotThrow_WhenContentPresenterChildIsAssigned()
        {
            var page = new ContentPage { Header = "Home" };
            var host = new PageNavigationHost { Page = page };
            var root = new TestRoot { Child = host };

            var exception = Record.Exception(() => root.LayoutManager.ExecuteInitialLayoutPass());

            Assert.Null(exception);
            Assert.NotNull(host.Presenter);
            Assert.Same(page, host.Presenter!.Child);
        }

        [Fact]
        public void ReplacingPage_ResetsOldPresenterChildSafeAreaPadding()
        {
            var first = new ContentPage { Header = "First" };
            var second = new ContentPage { Header = "Second" };
            var host = new PageNavigationHost { Page = first };
            var root = new TestRoot { Child = host };

            root.LayoutManager.ExecuteInitialLayoutPass();
            first.SafeAreaPadding = new Thickness(1, 2, 3, 4);

            var exception = Record.Exception(() => host.Page = second);

            Assert.Null(exception);
            Assert.Equal(default, first.SafeAreaPadding);
            Assert.NotNull(host.Presenter);
            Assert.Same(second, host.Presenter!.Child);
        }
    }

    public class SystemBackButtonTests : ScopedTestBase
    {
        [Fact]
        public void BackRequested_ForwardsToNestedCurrentPageOnce()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);
            var child = new ContentPage { Header = "Child" };
            var parent = new CarouselPage
            {
                Pages = new AvaloniaList<Page> { child }
            };
            var host = new PageNavigationHost { Page = parent };
            var window = new Window
            {
                Width = 400,
                Height = 300,
                Content = host
            };
            var raiseCount = 0;
            child.PageNavigationSystemBackButtonPressed += (_, e) =>
            {
                raiseCount++;
                e.Handled = true;
            };

            window.Show();

            Assert.Same(child, parent.CurrentPage);
            Assert.Same(parent, host.Presenter?.Child);

            var args = new RoutedEventArgs(TopLevel.BackRequestedEvent);
            window.RaiseEvent(args);

            Assert.Equal(1, raiseCount);
            Assert.True(args.Handled);
        }
    }
}
