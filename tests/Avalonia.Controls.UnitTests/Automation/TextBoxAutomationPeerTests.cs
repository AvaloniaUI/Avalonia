using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TextBoxAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void Value_Should_Be_Text_When_No_PasswordChar_Is_Set()
    {
        var target = new TextBox { Text = "secret" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("secret", peer.Value);
    }

    [Fact]
    public void Value_Should_Be_Masked_When_PasswordChar_Is_Set()
    {
        var target = new TextBox { PasswordChar = '*', Text = "secret" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("******", peer.Value);
    }

    [Fact]
    public void Value_Should_Be_Text_When_Password_Is_Revealed()
    {
        var target = new TextBox { PasswordChar = '*', RevealPassword = true, Text = "secret" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        Assert.Equal("secret", peer.Value);
    }

    [Fact]
    public void Changing_Text_Of_Password_Box_Should_Raise_ValueProperty_Changed_Event_With_Masked_Values()
    {
        var target = new TextBox { PasswordChar = '*', Text = "old" };
        var peer = ControlAutomationPeer.CreatePeerForElement(target);
        var changes = RecordValueChanges(peer);

        target.Text = "newer";

        var change = Assert.Single(changes);
        Assert.Equal("***", change.OldValue);
        Assert.Equal("*****", change.NewValue);
    }

    [Fact]
    public void Toggling_RevealPassword_Should_Raise_ValueProperty_Changed_Event()
    {
        var target = new TextBox { PasswordChar = '*', Text = "secret" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);
        var changes = RecordValueChanges((AutomationPeer)peer);

        target.RevealPassword = true;

        var change = Assert.Single(changes);
        Assert.Equal("******", change.OldValue);
        Assert.Equal("secret", change.NewValue);
        Assert.Equal(peer.Value, change.NewValue);
    }

    [Fact]
    public void Setting_PasswordChar_Should_Raise_ValueProperty_Changed_Event()
    {
        var target = new TextBox { Text = "secret" };
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);
        var changes = RecordValueChanges((AutomationPeer)peer);

        target.PasswordChar = '*';

        var change = Assert.Single(changes);
        Assert.Equal("secret", change.OldValue);
        Assert.Equal("******", change.NewValue);
        Assert.Equal(peer.Value, change.NewValue);
    }

    [Fact]
    public void SetValue_Preserves_Style_Binding()
    {
        var target = new TextBox();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);

        AutomationPeerAssert.PreservesStyleBinding(target, TextBox.TextProperty, "old", "new", () => peer.SetValue("new"));
    }

    private static List<AutomationPropertyChangedEventArgs> RecordValueChanges(AutomationPeer peer)
    {
        var result = new List<AutomationPropertyChangedEventArgs>();

        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ValuePatternIdentifiers.ValueProperty)
                result.Add(e);
        };

        return result;
    }
}
