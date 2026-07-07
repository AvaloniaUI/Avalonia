using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class MenuItemAutomationPeerTests : ScopedTestBase
{
    private static IToggleProvider GetProvider(MenuItem menuItem)
    {
        var provider = ControlAutomationPeer.CreatePeerForElement(menuItem).GetProvider<IToggleProvider>();
        Assert.NotNull(provider);
        return provider;
    }

    [Fact]
    public void Toggle_Provider_Is_Not_Exposed_When_ToggleType_None()
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem());

        Assert.Null(peer.GetProvider<IToggleProvider>());
    }

    [Theory]
    [InlineData(MenuItemToggleType.CheckBox)]
    [InlineData(MenuItemToggleType.Radio)]
    public void Toggle_Provider_Is_Exposed_For_Checkable_Items(MenuItemToggleType toggleType)
    {
        var peer = ControlAutomationPeer.CreatePeerForElement(new MenuItem { ToggleType = toggleType });

        Assert.NotNull(peer.GetProvider<IToggleProvider>());
    }

    [Fact]
    public void ToggleState_Reflects_IsChecked()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var provider = GetProvider(menuItem);

        Assert.Equal(ToggleState.Off, provider.ToggleState);
        menuItem.IsChecked = true;
        Assert.Equal(ToggleState.On, provider.ToggleState);
    }

    [Fact]
    public void Toggle_Flips_CheckBox_Item()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var provider = GetProvider(menuItem);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);

        provider.Toggle();
        Assert.False(menuItem.IsChecked);
    }

    [Fact]
    public void Toggle_Checks_But_Does_Not_Uncheck_Radio_Item()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.Radio };
        var provider = GetProvider(menuItem);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);

        provider.Toggle();
        Assert.True(menuItem.IsChecked);
    }

    [Fact]
    public void Toggle_Raises_ToggleState_PropertyChanged()
    {
        var menuItem = new MenuItem { ToggleType = MenuItemToggleType.CheckBox };
        var peer = ControlAutomationPeer.CreatePeerForElement(menuItem);
        var provider = GetProvider(menuItem);

        var raised = 0;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == TogglePatternIdentifiers.ToggleStateProperty)
            {
                Assert.Equal(ToggleState.Off, e.OldValue);
                Assert.Equal(ToggleState.On, e.NewValue);
                raised++;
            }
        };

        provider.Toggle();

        Assert.Equal(1, raised);
    }
}
