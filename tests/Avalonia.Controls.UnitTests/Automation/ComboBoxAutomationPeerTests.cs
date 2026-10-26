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
    public void Expand_Preserves_Style_Binding()
    {
        var comboBox = new ComboBox();
        var provider = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(comboBox);

        AutomationPeerAssert.PreservesStyleBinding(comboBox, ComboBox.IsDropDownOpenProperty, false, true, provider.Expand);
    }

    [Fact]
    public void Collapse_Preserves_Style_Binding()
    {
        var comboBox = new ComboBox();
        var provider = (IExpandCollapseProvider)ControlAutomationPeer.CreatePeerForElement(comboBox);

        AutomationPeerAssert.PreservesStyleBinding(comboBox, ComboBox.IsDropDownOpenProperty, true, false, provider.Collapse);
    }
}
