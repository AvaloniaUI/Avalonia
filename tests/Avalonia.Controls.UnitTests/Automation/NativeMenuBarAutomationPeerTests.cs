using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class NativeMenuBarAutomationPeerTests
{
    public class PeerCreation : ScopedTestBase
    {
        [Fact]
        public void Creates_NativeMenuBarAutomationPeer()
        {
            var control = new NativeMenuBar { Template = CreateTemplate() };
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.IsType<NativeMenuBarAutomationPeer>(peer);
        }

        [Fact]
        public void ControlType_Is_MenuBar()
        {
            var control = new NativeMenuBar { Template = CreateTemplate() };
            var peer = (NativeMenuBarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);

            Assert.Equal(AutomationControlType.MenuBar, peer.GetAutomationControlType());
        }
    }

    public class Children : ScopedTestBase
    {
        [Fact]
        public void Exposes_Realized_MenuItems_Via_VisualChildren()
        {
            var control = new NativeMenuBar { Template = CreateTemplate() };
            control.ApplyTemplate();
            var menu = Assert.IsType<Menu>(control.GetVisualChildren().Single());
            menu.ApplyTemplate();
            menu.Measure(Size.Infinity);
            menu.Arrange(new Rect(0, 0, 300, 30));
            control.Measure(Size.Infinity);
            control.Arrange(new Rect(0, 0, 300, 30));

            var peer = (NativeMenuBarAutomationPeer)ControlAutomationPeer.CreatePeerForElement(control);
            var presenterChildren = peer.GetChildren();

            Assert.Equal(2, presenterChildren.Count);
            Assert.All(presenterChildren, x => Assert.IsType<MenuItemAutomationPeer>(x));
            Assert.Equal("File", presenterChildren[0].GetName());
            Assert.Equal("Edit", presenterChildren[1].GetName());
        }
    }

    private static FuncControlTemplate CreateTemplate()
    {
        return new FuncControlTemplate((_, ns) =>
            new Menu
            {
                Name = "PART_NativeMenuPresenter",
                Items =
                {
                    new MenuItem { Header = "File" },
                    new MenuItem { Header = "Edit" },
                },
            }.RegisterInNameScope(ns));
    }
}
