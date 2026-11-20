using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TextBlockAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Name_Is_Text()
    {
        var target = new TextBlock { Text = "shown text" };

        Assert.Equal("shown text", GetName(target));
    }

    [Fact]
    public void Automation_Name_Wins_Over_Text()
    {
        var target = new TextBlock { Text = "shown text" };
        AutomationProperties.SetName(target, "explicit name");

        Assert.Equal("explicit name", GetName(target));
    }

    [Fact]
    public void Text_Change_Raises_Name_PropertyChanged()
    {
        var target = new TextBlock { Text = "old" };
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        var raised = 0;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutomationElementIdentifiers.NameProperty)
            {
                Assert.Equal("new", e.NewValue);
                raised++;
            }
        };

        target.Text = "new";

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Text_Change_Does_Not_Raise_Name_PropertyChanged_When_Automation_Name_Is_Set()
    {
        var target = new TextBlock { Text = "old" };
        AutomationProperties.SetName(target, "explicit name");
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        var raised = 0;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == AutomationElementIdentifiers.NameProperty)
            {
                raised++;
            }
        };

        target.Text = "new";

        Assert.Equal(0, raised);
    }

    private static string? GetName(Control control) =>
        ControlAutomationPeer.CreatePeerForElement(control).GetName();
}
