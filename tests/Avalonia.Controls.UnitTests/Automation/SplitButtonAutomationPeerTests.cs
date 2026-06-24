using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class SplitButtonAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Creates_SplitButtonAutomationPeer()
    {
        var target = new SplitButton();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsType<SplitButtonAutomationPeer>(peer);
    }

    [Fact]
    public void Implements_IExpandCollapseProvider()
    {
        var target = new SplitButton();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
    }

    [Fact]
    public void Implements_IInvokeProvider()
    {
        var target = new SplitButton();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<IInvokeProvider>(peer);
    }

    [Fact]
    public void ControlType_Is_SplitButton()
    {
        var target = new SplitButton();
        var peer = (SplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(AutomationControlType.SplitButton, peer.GetAutomationControlType());
    }

    [Fact]
    public void ClassName_Is_SplitButton()
    {
        var target = new SplitButton();
        var peer = (SplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("SplitButton", peer.GetClassName());
    }

    [Fact]
    public void ShowsMenu_Is_True()
    {
        var target = new SplitButton();
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.True(peer.ShowsMenu);
    }

    [Fact]
    public void Invoke_Triggers_Click()
    {
        var clicked = 0;
        var target = new SplitButton();
        var peer = (IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(target);

        target.Click += (_, _) => clicked++;
        peer.Invoke();

        Assert.Equal(1, clicked);
    }

    [Fact]
    public void ExpandCollapse_State_Tracks_Flyout()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var target = new SplitButton
            {
                Flyout = new Flyout()
            };
            var window = new Window
            {
                Content = target,
            };
            window.Show();

            var peer = (SplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

            Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);

            peer.Expand();
            Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);
            Assert.True(target.Flyout?.IsOpen);

            peer.Collapse();
            Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
            Assert.False(target.Flyout!.IsOpen);
        }
    }

    [Fact]
    public void ExpandCollapse_Raises_PropertyChanged()
    {
        using (UnitTestApplication.Start(TestServices.StyledWindow))
        {
            var target = new SplitButton
            {
                Flyout = new Flyout()
            };
            var window = new Window
            {
                Content = target,
            };
            window.Show();

            var peer = (SplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);
            AutomationPropertyChangedEventArgs? changed = null;
            peer.PropertyChanged += (_, e) =>
            {
                if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
                {
                    changed = e;
                }
            };

            peer.Expand();

            Assert.NotNull(changed);
            Assert.Equal(ExpandCollapseState.Collapsed, changed!.OldValue);
            Assert.Equal(ExpandCollapseState.Expanded, changed.NewValue);
        }
    }
}

public class ToggleSplitButtonAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Creates_ToggleSplitButtonAutomationPeer()
    {
        var target = new ToggleSplitButton();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsType<ToggleSplitButtonAutomationPeer>(peer);
    }

    [Fact]
    public void Implements_IToggleProvider()
    {
        var target = new ToggleSplitButton();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<IToggleProvider>(peer);
    }

    [Fact]
    public void ControlType_Is_SplitButton()
    {
        var target = new ToggleSplitButton();
        var peer = (SplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(AutomationControlType.SplitButton, peer.GetAutomationControlType());
    }

    [Fact]
    public void ClassName_Is_ToggleSplitButton()
    {
        var target = new ToggleSplitButton();
        var peer = (ToggleSplitButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("ToggleSplitButton", peer.GetClassName());
    }

    [Fact]
    public void Toggle_Changes_IsChecked_And_Fires_Click()
    {
        var clicked = 0;
        var target = new ToggleSplitButton();
        var peer = (IToggleProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(ToggleState.Off, peer.ToggleState);

        target.Click += (_, _) => clicked++;
        peer.Toggle();

        Assert.True(target.IsChecked);
        Assert.Equal(ToggleState.On, peer.ToggleState);
        Assert.Equal(1, clicked);
    }
}
