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
    }
}
