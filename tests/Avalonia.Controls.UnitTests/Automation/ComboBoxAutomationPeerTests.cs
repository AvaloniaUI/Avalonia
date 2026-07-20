using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class ComboBoxAutomationPeerTests : ScopedTestBase
{
    private static IValueProvider Value(ComboBox comboBox)
        => Assert.IsAssignableFrom<IValueProvider>(ControlAutomationPeer.CreatePeerForElement(comboBox));

    [Fact]
    public void Non_Editable_ComboBox_Is_ReadOnly()
    {
        var provider = Value(new ComboBox());

        Assert.True(provider.IsReadOnly);
    }

    [Fact]
    public void Editable_ComboBox_Is_Not_ReadOnly()
    {
        var provider = Value(new ComboBox { IsEditable = true });

        Assert.False(provider.IsReadOnly);
    }

    [Fact]
    public void Value_Returns_Text_When_Editable()
    {
        var provider = Value(new ComboBox { IsEditable = true, Text = "hello" });

        Assert.Equal("hello", provider.Value);
    }

    [Fact]
    public void SetValue_Updates_Text_When_Editable()
    {
        var comboBox = new ComboBox { IsEditable = true };
        var provider = Value(comboBox);

        provider.SetValue("typed");

        Assert.Equal("typed", comboBox.Text);
        Assert.Equal("typed", provider.Value);
    }

    [Fact]
    public void SetValue_Throws_When_Not_Editable()
    {
        var provider = Value(new ComboBox());

        Assert.Throws<System.InvalidOperationException>(() => provider.SetValue("x"));
    }

    [Fact]
    public void Text_Change_Raises_Value_PropertyChanged()
    {
        var comboBox = new ComboBox { IsEditable = true };
        var peer = ControlAutomationPeer.CreatePeerForElement(comboBox);

        var raised = 0;
        peer.PropertyChanged += (_, e) =>
        {
            if (e.Property == ValuePatternIdentifiers.ValueProperty)
            {
                Assert.Equal("abc", e.NewValue);
                raised++;
            }
        };

        comboBox.Text = "abc";

        Assert.Equal(1, raised);
    }
}
