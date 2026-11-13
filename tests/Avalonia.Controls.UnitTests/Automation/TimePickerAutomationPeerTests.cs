using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class TimePickerAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void SetValue_Preserves_Style_Binding()
    {
        var target = new TimePicker();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);
        var time = new TimeSpan(13, 37, 0);

        AutomationPeerAssert.PreservesStyleBinding(target, TimePicker.SelectedTimeProperty, null, time,
            () => peer.SetValue(time.ToString()));
    }
}
