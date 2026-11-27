using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ContentControlAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Name_Is_String_Content()
    {
        var target = new Button { Content = "Open" };

        Assert.Equal("Open", GetName(target));
    }

    [Fact]
    public void Name_Is_ToString_Of_Data_Object_Content()
    {
        var target = new Button { Content = new NamedItem() };

        Assert.Equal("Named item", GetName(target));
    }

    [Fact]
    public void Name_Is_Not_Type_Name_When_Content_Is_A_Control()
    {
        var target = new Button { Content = new Path() };

        Assert.True(string.IsNullOrEmpty(GetName(target)));
    }

    [Fact]
    public void Name_Comes_From_TextBlock_In_Content()
    {
        var target = new Button
        {
            Content = new StackPanel
            {
                Children =
                {
                    new Path(),
                    new TextBlock { Text = "Open" },
                }
            }
        };

        Assert.Equal("Open", GetName(target));
    }

    [Fact]
    public void Automation_Name_Wins_Over_Content()
    {
        var target = new Button { Content = new TextBlock { Text = "Open" } };
        AutomationProperties.SetName(target, "Custom");

        Assert.Equal("Custom", GetName(target));
    }

    private static string? GetName(Control control) =>
        ControlAutomationPeer.CreatePeerForElement(control).GetName();

    private class NamedItem
    {
        public override string ToString() => "Named item";
    }
}
