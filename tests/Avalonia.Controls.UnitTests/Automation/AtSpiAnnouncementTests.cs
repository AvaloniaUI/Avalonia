using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.FreeDesktop.AtSpi;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AtSpiAnnouncementTests : ScopedTestBase
    {
        [Theory]
        [InlineData(AutomationLiveSetting.Polite, 1)]
        [InlineData(AutomationLiveSetting.Assertive, 2)]
        public void Live_Region_Announces_Its_New_Name_With_Its_Politeness(
            AutomationLiveSetting liveSetting, int expectedPoliteness)
        {
            var control = new TextBlock { Text = "Building" };
            AutomationProperties.SetLiveSetting(control, liveSetting);
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            control.Text = "Build succeeded";

            Assert.True(AtSpiNode.TryGetLiveAnnouncement(peer, out var text, out var politeness));
            Assert.Equal("Build succeeded", text);
            Assert.Equal(expectedPoliteness, (int)politeness);
        }

        [Fact]
        public void Element_That_Is_Not_A_Live_Region_Announces_Nothing()
        {
            var control = new TextBlock { Text = "Build succeeded" };
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.False(AtSpiNode.TryGetLiveAnnouncement(peer, out _, out _));
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Live_Region_Without_A_Name_Announces_Nothing(string name)
        {
            var control = new TextBlock { Text = name };
            AutomationProperties.SetLiveSetting(control, AutomationLiveSetting.Polite);
            var peer = ControlAutomationPeer.CreatePeerForElement(control);

            Assert.False(AtSpiNode.TryGetLiveAnnouncement(peer, out _, out _));
        }
    }
}
