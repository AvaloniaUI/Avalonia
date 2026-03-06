using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class NavigationEventArgsTests
{
    public class NavigatedToEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var prev = new ContentPage { Header = "Prev" };
            var args = new NavigatedToEventArgs(prev, NavigationType.Push);
            Assert.Same(prev, args.PreviousPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public void NullPreviousPage_IsAllowed()
        {
            var args = new NavigatedToEventArgs(null, NavigationType.PopToRoot);
            Assert.Null(args.PreviousPage);
        }
    }

    public class NavigatedFromEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var dest = new ContentPage { Header = "Dest" };
            var args = new NavigatedFromEventArgs(dest, NavigationType.Pop);
            Assert.Same(dest, args.DestinationPage);
            Assert.Equal(NavigationType.Pop, args.NavigationType);
        }
    }

    public class NavigatingFromEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Cancel_DefaultIsFalse()
        {
            var args = new NavigatingFromEventArgs(null, NavigationType.Push);
            Assert.False(args.Cancel);
        }

        [Fact]
        public void Cancel_CanBeSetTrue()
        {
            var args = new NavigatingFromEventArgs(null, NavigationType.Push) { Cancel = true };
            Assert.True(args.Cancel);
        }
    }

    public class NavigationEventArgsConstructionTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var page = new ContentPage();
            var args = new NavigationEventArgs(page, NavigationType.Push);
            Assert.Same(page, args.Page);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }
    }

    public class ModalPushedEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var modal = new ContentPage();
            var args = new ModalPushedEventArgs(modal);
            Assert.Same(modal, args.Modal);
        }
    }

    public class ModalPoppedEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var modal = new ContentPage();
            var args = new ModalPoppedEventArgs(modal);
            Assert.Same(modal, args.Modal);
        }
    }

    public class PageSelectionChangedEventArgsTests : ScopedTestBase
    {
        [Fact]
        public void Properties_RoundTrip()
        {
            var prev = new ContentPage { Header = "Tab 1" };
            var current = new ContentPage { Header = "Tab 2" };
            var args = new PageSelectionChangedEventArgs(prev, current);
            Assert.Same(prev, args.PreviousPage);
            Assert.Same(current, args.CurrentPage);
        }
    }

    public class NavigationTypeEnumTests : ScopedTestBase
    {
        [Fact]
        public void AllValuesAreDefined()
        {
            var values = System.Enum.GetValues<NavigationType>();
            Assert.Contains(NavigationType.Push, values);
            Assert.Contains(NavigationType.Pop, values);
            Assert.Contains(NavigationType.PopToRoot, values);
            Assert.Contains(NavigationType.Insert, values);
            Assert.Contains(NavigationType.Remove, values);
            Assert.Contains(NavigationType.Replace, values);
        }
    }
}
