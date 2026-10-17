using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class AutomationPeerCollectionTests : ScopedTestBase
{
    [Fact]
    public void Menu_Items_Report_Their_Position_And_Set_Size()
    {
        var first = new MenuItem { Header = "New File" };
        var second = new MenuItem { Header = "Open File" };
        var third = new MenuItem { Header = "Exit" };
        var menu = new MenuItem { Items = { first, second, third } };

        AssertSetPosition(first, 1, 3);
        AssertSetPosition(second, 2, 3);
        AssertSetPosition(third, 3, 3);
    }

    [Fact]
    public void List_Items_Report_Their_Position_And_Set_Size()
    {
        var first = new ListBoxItem { Content = "First" };
        var second = new ListBoxItem { Content = "Second" };
        var third = new ListBoxItem { Content = "Third" };
        var list = new ListBox { Items = { first, second, third } };

        AssertSetPosition(first, 1, 3);
        AssertSetPosition(second, 2, 3);
        AssertSetPosition(third, 3, 3);
    }

    [Fact]
    public void Explicit_Position_And_Size_Override_The_Collection_Values()
    {
        var item = new ListBoxItem { Content = "Custom position" };
        AutomationProperties.SetPositionInSet(item, 7);
        AutomationProperties.SetSizeOfSet(item, 12);
        var list = new ListBox { Items = { item, new ListBoxItem() } };

        AssertSetPosition(item, 7, 12);
    }

    private static void AssertSetPosition(Control item, int position, int size)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(position, peer.GetPositionInSet());
        Assert.Equal(size, peer.GetSizeOfSet());
    }
}
