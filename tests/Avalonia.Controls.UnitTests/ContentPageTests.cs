using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class ContentPageTests
{
    public class PageBaseDefaults : ScopedTestBase
    {
        [Fact]
        public void Header_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.Header);
        }

        [Theory]
        [InlineData("Home")]
        [InlineData("Settings")]
        [InlineData("")]
        public void Header_AcceptsStringValue(string title)
        {
            var page = new ContentPage { Header = title };
            Assert.Equal(title, page.Header);
        }

        [Fact]
        public void Header_AcceptsControlValue()
        {
            var label = new TextBlock { Text = "Custom Title" };
            var page = new ContentPage { Header = label };
            Assert.Same(label, page.Header);
        }

        [Fact]
        public void Icon_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.Icon);
        }

        [Fact]
        public void Icon_RoundTrips()
        {
            var icon = new Image();
            var page = new ContentPage { Icon = icon };
            Assert.Same(icon, page.Icon);
        }

        [Fact]
        public void SafeAreaPadding_DefaultIsZero()
        {
            var page = new ContentPage();
            Assert.Equal(default(Thickness), page.SafeAreaPadding);
        }

        [Fact]
        public void SafeAreaPadding_RoundTrips()
        {
            var page = new ContentPage();
            var padding = new Thickness(10, 20, 10, 30);
            page.SafeAreaPadding = padding;
            Assert.Equal(padding, page.SafeAreaPadding);
        }

        [Fact]
        public void IsInNavigationPage_DefaultIsFalse()
        {
            var page = new ContentPage();
            Assert.False(page.IsInNavigationPage);
        }

        [Fact]
        public void Navigation_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.Navigation);
        }

        [Fact]
        public void CurrentPage_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.CurrentPage);
        }
    }

    public class ContentPageProperties : ScopedTestBase
    {
        [Fact]
        public void Content_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.Content);
        }

        [Fact]
        public void Content_AcceptsStringValue()
        {
            var page = new ContentPage { Content = "Hello" };
            Assert.Equal("Hello", page.Content);
        }

        [Fact]
        public void Content_AcceptsControl()
        {
            var ctrl = new Button();
            var page = new ContentPage { Content = ctrl };
            Assert.Same(ctrl, page.Content);
        }

        [Fact]
        public void AutomaticallyApplySafeAreaPadding_DefaultIsTrue()
        {
            var page = new ContentPage();
            Assert.True(page.AutomaticallyApplySafeAreaPadding);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AutomaticallyApplySafeAreaPadding_RoundTrips(bool value)
        {
            var page = new ContentPage { AutomaticallyApplySafeAreaPadding = value };
            Assert.Equal(value, page.AutomaticallyApplySafeAreaPadding);
        }

        [Fact]
        public void HorizontalContentAlignment_DefaultIsStretch()
        {
            var page = new ContentPage();
            Assert.Equal(HorizontalAlignment.Stretch, page.HorizontalContentAlignment);
        }

        [Theory]
        [InlineData(HorizontalAlignment.Left)]
        [InlineData(HorizontalAlignment.Center)]
        [InlineData(HorizontalAlignment.Right)]
        [InlineData(HorizontalAlignment.Stretch)]
        public void HorizontalContentAlignment_RoundTrips(HorizontalAlignment value)
        {
            var page = new ContentPage { HorizontalContentAlignment = value };
            Assert.Equal(value, page.HorizontalContentAlignment);
        }

        [Fact]
        public void VerticalContentAlignment_DefaultIsStretch()
        {
            var page = new ContentPage();
            Assert.Equal(VerticalAlignment.Stretch, page.VerticalContentAlignment);
        }

        [Theory]
        [InlineData(VerticalAlignment.Top)]
        [InlineData(VerticalAlignment.Center)]
        [InlineData(VerticalAlignment.Bottom)]
        [InlineData(VerticalAlignment.Stretch)]
        public void VerticalContentAlignment_RoundTrips(VerticalAlignment value)
        {
            var page = new ContentPage { VerticalContentAlignment = value };
            Assert.Equal(value, page.VerticalContentAlignment);
        }

        [Fact]
        public void ContentTemplate_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.ContentTemplate);
        }

        [Fact]
        public void ContentTemplate_RoundTrips()
        {
            var template = new FuncDataTemplate<object>((_, _) => null);
            var page = new ContentPage { ContentTemplate = template };
            Assert.Same(template, page.ContentTemplate);
        }
    }

    public class LogicalChildren : ScopedTestBase
    {
        [Fact]
        public void Content_SetControl_AddsToLogicalChildren()
        {
            var page = new ContentPage();
            var child = new Button();
            page.Content = child;
            Assert.Contains(child, ((ILogical)page).LogicalChildren);
        }

        [Fact]
        public void Content_Replaced_OldChildRemovedNewChildAdded()
        {
            var page = new ContentPage();
            var first = new Button();
            var second = new TextBlock();
            page.Content = first;
            page.Content = second;

            Assert.DoesNotContain(first, ((ILogical)page).LogicalChildren);
            Assert.Contains(second, ((ILogical)page).LogicalChildren);
        }

        [Fact]
        public void Content_SetToNull_RemovesOldChild()
        {
            var page = new ContentPage();
            var child = new Button();
            page.Content = child;
            page.Content = null;

            Assert.DoesNotContain(child, ((ILogical)page).LogicalChildren);
        }

        [Fact]
        public void Content_SetToString_DoesNotAddToLogicalChildren()
        {
            var page = new ContentPage { Content = "hello" };
            Assert.Empty(((ILogical)page).LogicalChildren);
        }
    }

    public class CommandBarProperties : ScopedTestBase
    {
        [Fact]
        public void TopCommandBar_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.TopCommandBar);
        }

        [Fact]
        public void TopCommandBar_RoundTrips()
        {
            var bar = new Button();
            var page = new ContentPage { TopCommandBar = bar };
            Assert.Same(bar, page.TopCommandBar);
        }

        [Fact]
        public void BottomCommandBar_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.BottomCommandBar);
        }

        [Fact]
        public void BottomCommandBar_RoundTrips()
        {
            var bar = new Button();
            var page = new ContentPage { BottomCommandBar = bar };
            Assert.Same(bar, page.BottomCommandBar);
        }

        [Fact]
        public void TopCommandBar_And_BottomCommandBar_AreIndependent()
        {
            var top = new Button();
            var bottom = new TextBlock();
            var page = new ContentPage { TopCommandBar = top, BottomCommandBar = bottom };
            Assert.Same(top, page.TopCommandBar);
            Assert.Same(bottom, page.BottomCommandBar);
        }
    }

    public class NavigationEventArgTypes : ScopedTestBase
    {
        [Fact]
        public void NavigatedToEventArgs_Properties()
        {
            var prev = new ContentPage { Header = "Prev" };
            var args = new NavigatedToEventArgs(prev, NavigationType.Push);
            Assert.Same(prev, args.PreviousPage);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public void NavigatedToEventArgs_NullPreviousPage_IsAllowed()
        {
            var args = new NavigatedToEventArgs(null, NavigationType.PopToRoot);
            Assert.Null(args.PreviousPage);
        }

        [Fact]
        public void NavigatedFromEventArgs_Properties()
        {
            var dest = new ContentPage { Header = "Dest" };
            var args = new NavigatedFromEventArgs(dest, NavigationType.Pop);
            Assert.Same(dest, args.DestinationPage);
            Assert.Equal(NavigationType.Pop, args.NavigationType);
        }

        [Fact]
        public void NavigatingFromEventArgs_DefaultCancelIsFalse()
        {
            var args = new NavigatingFromEventArgs(null, NavigationType.Push);
            Assert.False(args.Cancel);
        }

        [Fact]
        public void NavigatingFromEventArgs_Cancel_CanBeSetTrue()
        {
            var args = new NavigatingFromEventArgs(null, NavigationType.Push) { Cancel = true };
            Assert.True(args.Cancel);
        }

        [Fact]
        public void NavigationEventArgs_Properties()
        {
            var page = new ContentPage();
            var args = new NavigationEventArgs(page, NavigationType.Push);
            Assert.Same(page, args.Page);
            Assert.Equal(NavigationType.Push, args.NavigationType);
        }

        [Fact]
        public void ModalPushedEventArgs_Properties()
        {
            var modal = new ContentPage();
            var args = new ModalPushedEventArgs(modal);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public void ModalPoppedEventArgs_Properties()
        {
            var modal = new ContentPage();
            var args = new ModalPoppedEventArgs(modal);
            Assert.Same(modal, args.Modal);
        }

        [Fact]
        public void PageSelectionChangedEventArgs_Properties()
        {
            var prev = new ContentPage { Header = "Tab 1" };
            var current = new ContentPage { Header = "Tab 2" };
            var args = new PageSelectionChangedEventArgs(prev, current);
            Assert.Same(prev, args.PreviousPage);
            Assert.Same(current, args.CurrentPage);
        }
    }

    public class NavigationTypeEnum : ScopedTestBase
    {
        [Fact]
        public void NavigationType_AllValuesAreDefined()
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

    public class VirtualOverrides : ScopedTestBase
    {
        [Fact]
        public void OnSystemBackButtonPressed_DefaultReturnsFalse()
        {
            var page = new TestableContentPage();
            Assert.False(page.CallOnSystemBackButtonPressed());
        }

        [Fact]
        public void OnSystemBackButtonPressed_Override_ReturnsTrue()
        {
            var page = new BackButtonHandlingPage();
            Assert.True(page.CallOnSystemBackButtonPressed());
        }

        private class TestableContentPage : ContentPage
        {
            public bool CallOnSystemBackButtonPressed() => OnSystemBackButtonPressed();
        }

        private class BackButtonHandlingPage : ContentPage
        {
            public bool CallOnSystemBackButtonPressed() => OnSystemBackButtonPressed();
            protected override bool OnSystemBackButtonPressed() => true;
        }
    }
}
