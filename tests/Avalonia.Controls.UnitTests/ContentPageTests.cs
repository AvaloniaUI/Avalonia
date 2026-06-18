using System;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public class ContentPageTests
{
    public class PageDefaults : ScopedTestBase
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
        public void Header_SetString_RoundTrips(string title)
        {
            var page = new ContentPage { Header = title };
            Assert.Equal(title, page.Header);
        }

        [Fact]
        public void Header_SetControl_RoundTrips()
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

    public class ContentPropertyTests : ScopedTestBase
    {
        [Fact]
        public void Content_DefaultIsNull()
        {
            var page = new ContentPage();
            Assert.Null(page.Content);
        }

        [Fact]
        public void Content_SetString_RoundTrips()
        {
            var page = new ContentPage { Content = "Hello" };
            Assert.Equal("Hello", page.Content);
        }

        [Fact]
        public void Content_SetControl_RoundTrips()
        {
            var ctrl = new Button();
            var page = new ContentPage { Content = ctrl };
            Assert.Same(ctrl, page.Content);
        }

        [Theory]
        [InlineData(typeof(ContentPage))]
        [InlineData(typeof(NavigationPage))]
        [InlineData(typeof(TabbedPage))]
        public void Content_SetAnyPageSubtype_ThrowsInvalidOperationException(Type pageType)
        {
            var host = new ContentPage();
            var child = (Page)Activator.CreateInstance(pageType)!;
            Assert.Throws<InvalidOperationException>(() => host.Content = child);
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

    public class LogicalChildrenTests : ScopedTestBase
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

    public class CommandBarPropertyTests : ScopedTestBase
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

    public class SystemBackButtonTests : ScopedTestBase
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
