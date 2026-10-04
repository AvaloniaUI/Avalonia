using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.FreeDesktop.AtSpi;
using Avalonia.FreeDesktop.AtSpi.Handlers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation
{
    public class AtSpiAccessibleHandlerTests : ScopedTestBase
    {
        private const uint HeadingRole = 83;
        private const int DocumentRole = 82;
        private const int LinkRole = 88;
        private const int TreeItemRole = 91;
        private const int TitleBarRole = 104;

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public async Task Exposes_Heading_Role_And_Level(int level)
        {
            var control = new TextBlock { Text = "Section" };
            AutomationProperties.SetHeadingLevel(control, level);
            await using var server = new AtSpiServer();
            var handler = CreateHandler(control, server);

            Assert.Equal(HeadingRole, (uint)AtSpiRole.Heading);
            Assert.Equal((uint)AtSpiRole.Heading, await handler.GetRoleAsync());
            Assert.Equal("heading", await handler.GetRoleNameAsync());
            Assert.Equal("heading", await handler.GetLocalizedRoleNameAsync());
            Assert.Equal(level.ToString(CultureInfo.InvariantCulture), (await handler.GetAttributesAsync())["level"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task Unset_Heading_Preserves_Label_Role(int level)
        {
            var control = new TextBlock { Text = "Label" };
            AutomationProperties.SetHeadingLevel(control, level);
            await using var server = new AtSpiServer();
            var handler = CreateHandler(control, server);

            Assert.Equal((uint)AtSpiRole.Label, await handler.GetRoleAsync());
            Assert.Equal("label", await handler.GetRoleNameAsync());
            Assert.False((await handler.GetAttributesAsync()).ContainsKey("level"));
        }

        [Fact]
        public async Task Exposes_List_Position_And_Size()
        {
            var control = new ListBoxItem();
            AutomationProperties.SetPositionInSet(control, 3);
            AutomationProperties.SetSizeOfSet(control, 10);
            await using var server = new AtSpiServer();
            var handler = CreateHandler(control, server);
            var attributes = await handler.GetAttributesAsync();

            Assert.Equal("3", attributes["posinset"]);
            Assert.Equal("10", attributes["setsize"]);
            Assert.Equal((uint)AtSpiRole.ListItem, await handler.GetRoleAsync());
        }

        [Theory]
        [InlineData(AutomationControlType.Document, DocumentRole)]
        [InlineData(AutomationControlType.Hyperlink, LinkRole)]
        [InlineData(AutomationControlType.TreeItem, TreeItemRole)]
        [InlineData(AutomationControlType.TitleBar, TitleBarRole)]
        public void Uses_AtSpi_Role_Ids(AutomationControlType controlType, int expected)
        {
            Assert.Equal(expected, (int)AtSpiNode.ToAtSpiRole(controlType));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(0)]
        public async Task Omits_Unset_List_Position_And_Size(int value)
        {
            var control = new ListBoxItem();
            AutomationProperties.SetPositionInSet(control, value);
            AutomationProperties.SetSizeOfSet(control, value);
            await using var server = new AtSpiServer();
            var attributes = await CreateHandler(control, server).GetAttributesAsync();

            Assert.False(attributes.ContainsKey("posinset"));
            Assert.False(attributes.ContainsKey("setsize"));
        }

        [Theory]
        [InlineData(3, -1, "3", null)]
        [InlineData(-1, 10, null, "10")]
        public async Task Exposes_List_Position_And_Size_Independently(
            int position, int size, string? expectedPosition, string? expectedSize)
        {
            var control = new ListBoxItem();
            AutomationProperties.SetPositionInSet(control, position);
            AutomationProperties.SetSizeOfSet(control, size);
            await using var server = new AtSpiServer();
            var attributes = await CreateHandler(control, server).GetAttributesAsync();

            Assert.Equal(expectedPosition, attributes.TryGetValue("posinset", out var actualPosition) ? actualPosition : null);
            Assert.Equal(expectedSize, attributes.TryGetValue("setsize", out var actualSize) ? actualSize : null);
        }

        [Fact]
        public async Task Reads_Updated_Metadata()
        {
            var control = new TextBlock { Text = "Section" };
            await using var server = new AtSpiServer();
            var handler = CreateHandler(control, server);
            Assert.False((await handler.GetAttributesAsync()).ContainsKey("level"));

            AutomationProperties.SetHeadingLevel(control, 2);
            AutomationProperties.SetPositionInSet(control, 1);
            AutomationProperties.SetSizeOfSet(control, 4);
            var attributes = await handler.GetAttributesAsync();

            Assert.Equal("2", attributes["level"]);
            Assert.Equal("1", attributes["posinset"]);
            Assert.Equal("4", attributes["setsize"]);
            Assert.Equal((uint)AtSpiRole.Heading, await handler.GetRoleAsync());

            control.ClearValue(AutomationProperties.HeadingLevelProperty);
            control.ClearValue(AutomationProperties.PositionInSetProperty);
            control.ClearValue(AutomationProperties.SizeOfSetProperty);
            attributes = await handler.GetAttributesAsync();

            Assert.False(attributes.ContainsKey("level"));
            Assert.False(attributes.ContainsKey("posinset"));
            Assert.False(attributes.ContainsKey("setsize"));
            Assert.Equal((uint)AtSpiRole.Label, await handler.GetRoleAsync());
        }

        private static AtSpiAccessibleHandler CreateHandler(Control control, AtSpiServer server)
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(control);
            return new AtSpiAccessibleHandler(server, server.GetOrCreateNode(peer));
        }
    }
}
