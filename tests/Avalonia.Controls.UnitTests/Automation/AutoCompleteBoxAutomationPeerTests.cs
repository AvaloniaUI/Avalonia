using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class AutoCompleteBoxAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Creates_AutoCompleteBoxAutomationPeer()
    {
        var target = new AutoCompleteBox();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsType<AutoCompleteBoxAutomationPeer>(peer);
    }

    [Fact]
    public void Implements_IExpandCollapse_And_IValue_Providers()
    {
        var target = new AutoCompleteBox();
        var peer = ControlAutomationPeer.CreatePeerForElement(target);

        Assert.IsAssignableFrom<IExpandCollapseProvider>(peer);
        Assert.IsAssignableFrom<IValueProvider>(peer);
        Assert.False(peer is IInvokeProvider);
    }

    [Fact]
    public void ControlType_Is_Group_And_ClassName_Is_AutoCompleteBox()
    {
        var target = new AutoCompleteBox();
        var peer = (AutoCompleteBoxAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Equal(nameof(AutoCompleteBox), peer.GetClassName());
    }

    [Fact]
    public void ExpandCollapse_Tracks_IsDropDownOpen()
    {
        var target = new AutoCompleteBox
        {
            ItemsSource = new[] { "alpha" },
            Text = "a"
        };
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);
        Assert.True(peer.ShowsMenu);

        target.IsDropDownOpen = false;
        Assert.False(target.IsDropDownOpen);

        peer.Expand();
        Assert.True(target.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);

        peer.Collapse();
        Assert.False(target.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Collapsed, peer.ExpandCollapseState);
    }

    [Fact]
    public void Value_Tracks_And_Sets_Text()
    {
        var target = new AutoCompleteBox { Text = "one" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("one", peer.Value);
        peer.SetValue("two");
        Assert.Equal("two", target.Text);
        Assert.Equal("two", ((IValueProvider)peer).Value);
    }

    [Fact]
    public void ValueProvider_IsMutable()
    {
        var target = new AutoCompleteBox();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.False(peer.IsReadOnly);
    }

    [Fact]
    public void Property_Change_Events_Raise_For_DropDown_And_Text()
    {
        var target = new AutoCompleteBox();
        var peer = (AutoCompleteBoxAutomationPeer)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPropertyChangedEventArgs? expandCollapseChanged = null;
        AutomationPropertyChangedEventArgs? valueChanged = null;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty)
            {
                expandCollapseChanged = e;
            }
            else if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                valueChanged = e;
            }
        };

        target.IsDropDownOpen = true;
        Assert.NotNull(expandCollapseChanged);
        Assert.Equal(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, expandCollapseChanged!.Property);
        Assert.Equal(ExpandCollapseState.Collapsed, expandCollapseChanged.OldValue);
        Assert.Equal(ExpandCollapseState.Expanded, expandCollapseChanged.NewValue);

        target.Text = "query";
        Assert.NotNull(valueChanged);
        Assert.Equal(ValuePatternIdentifiers.ValueProperty, valueChanged!.Property);
        Assert.Equal(string.Empty, valueChanged.OldValue);
        Assert.Equal("query", valueChanged.NewValue);
    }

    [Fact]
    public void Expand_Preserves_Style_Binding()
    {
        var target = new AutoCompleteBox { MinimumPrefixLength = 0, ItemsSource = new[] { "a", "b" } };
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, AutoCompleteBox.IsDropDownOpenProperty, false, true, peer.Expand);
    }

    [Fact]
    public void Collapse_Preserves_Style_Binding()
    {
        var target = new AutoCompleteBox { MinimumPrefixLength = 0, ItemsSource = new[] { "a", "b" } };
        var peer = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, AutoCompleteBox.IsDropDownOpenProperty, true, false, peer.Collapse);
    }

    [Fact]
    public void SetValue_Preserves_Style_Binding()
    {
        var target = new AutoCompleteBox();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, AutoCompleteBox.TextProperty, "old", "new", () => peer.SetValue("new"));
    }
}
