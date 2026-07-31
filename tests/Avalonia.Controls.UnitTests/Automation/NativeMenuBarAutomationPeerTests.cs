using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
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
