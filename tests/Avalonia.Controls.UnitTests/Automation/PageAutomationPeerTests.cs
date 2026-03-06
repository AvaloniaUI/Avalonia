using Avalonia.Automation.Peers;
using Avalonia.UnitTests;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Automation;

public class PageAutomationPeerTests
{
    public class ContentPagePeer : ScopedTestBase
    {
        [Fact]
        public void Creates_ContentPageAutomationPeer()
        {
            var page = new ContentPage();
            var peer = ControlAutomationPeer.CreatePeerForElement(page);

            Assert.IsType<ContentPageAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_Pane()
        {
            var page = new ContentPage();
            var peer = (ContentPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        }

        [Fact]
        public void Name_Returns_String_Header()
        {
            var page = new ContentPage { Header = "Settings" };
            var peer = (ContentPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Settings", peer.GetName());
        }

        [Fact]
        public void Name_Returns_ToString_For_Non_String_Header()
        {
            var page = new ContentPage { Header = 42 };
            var peer = (ContentPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("42", peer.GetName());
        }

        [Fact]
        public void Name_Is_Empty_When_No_Header()
        {
            var page = new ContentPage();
            var peer = (ContentPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.True(string.IsNullOrEmpty(peer.GetName()));
        }
    }

    public class TabbedPagePeer : ScopedTestBase
    {
        [Fact]
        public void Creates_TabbedPageAutomationPeer()
        {
            var page = new TabbedPage();
            var peer = ControlAutomationPeer.CreatePeerForElement(page);

            Assert.IsType<TabbedPageAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_Pane()
        {
            var page = new TabbedPage();
            var peer = (TabbedPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        }

        [Fact]
        public void Name_Returns_String_Header()
        {
            var page = new TabbedPage { Header = "Main" };
            var peer = (TabbedPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Main", peer.GetName());
        }

        [Fact]
        public void Name_Returns_ToString_For_Non_String_Header()
        {
            var page = new TabbedPage { Header = 42 };
            var peer = (TabbedPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("42", peer.GetName());
        }

        [Fact]
        public void Name_Is_Empty_When_No_Header()
        {
            var page = new TabbedPage();
            var peer = (TabbedPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.True(string.IsNullOrEmpty(peer.GetName()));
        }
    }

    public class NavigationPagePeer : ScopedTestBase
    {
        [Fact]
        public void Creates_NavigationPageAutomationPeer()
        {
            var page = new NavigationPage();
            var peer = ControlAutomationPeer.CreatePeerForElement(page);

            Assert.IsType<NavigationPageAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_Pane()
        {
            var page = new NavigationPage();
            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        }

        [Fact]
        public void Name_Returns_Own_Header_When_Set()
        {
            var page = new NavigationPage { Header = "Navigation" };
            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Navigation", peer.GetName());
        }

        [Fact]
        public void Name_Returns_ToString_For_Non_String_Header()
        {
            var page = new NavigationPage { Header = 42 };
            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("42", peer.GetName());
        }

        [Fact]
        public void Name_Prioritizes_Own_Header_Over_CurrentPage_Header()
        {
            var inner = new ContentPage { Header = "Details" };
            var page = new NavigationPage { Header = "Navigation" };
            page.SetCurrentValue(Page.CurrentPageProperty, inner);

            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Navigation", peer.GetName());
        }

        [Fact]
        public void Name_Falls_Back_To_CurrentPage_Header()
        {
            var inner = new ContentPage { Header = "Details" };
            var page = new NavigationPage();
            page.SetCurrentValue(Page.CurrentPageProperty, inner);

            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Details", peer.GetName());
        }

        [Fact]
        public void Name_Is_Empty_When_No_Header_And_No_CurrentPage()
        {
            var page = new NavigationPage();
            var peer = (NavigationPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.True(string.IsNullOrEmpty(peer.GetName()));
        }
    }

    public class DrawerPagePeer : ScopedTestBase
    {
        [Fact]
        public void Creates_DrawerPageAutomationPeer()
        {
            var page = new DrawerPage();
            var peer = ControlAutomationPeer.CreatePeerForElement(page);

            Assert.IsType<DrawerPageAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_Pane()
        {
            var page = new DrawerPage();
            var peer = (DrawerPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        }

        [Fact]
        public void Name_Returns_String_Header()
        {
            var page = new DrawerPage { Header = "Menu" };
            var peer = (DrawerPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("Menu", peer.GetName());
        }

        [Fact]
        public void Name_Returns_ToString_For_Non_String_Header()
        {
            var page = new DrawerPage { Header = 42 };
            var peer = (DrawerPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.Equal("42", peer.GetName());
        }

        [Fact]
        public void Name_Is_Empty_When_No_Header()
        {
            var page = new DrawerPage();
            var peer = (DrawerPageAutomationPeer)ControlAutomationPeer.CreatePeerForElement(page);

            Assert.True(string.IsNullOrEmpty(peer.GetName()));
        }
    }
}
