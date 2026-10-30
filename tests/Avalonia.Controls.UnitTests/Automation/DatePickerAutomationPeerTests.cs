using System;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

public class DatePickerAutomationPeerTests : ScopedTestBase
{
    [Fact]
    public void SetValue_Preserves_Style_Binding()
    {
        var target = new DatePicker();
        var peer = (IValueProvider)ControlAutomationPeer.CreatePeerForElement(target);
        var date = new DateTimeOffset(2026, 6, 5, 0, 0, 0, TimeSpan.Zero);

        AutomationPeerAssert.PreservesStyleBinding(
            target,
            DatePicker.SelectedDateProperty,
            null,
            date,
            () => peer.SetValue(date.ToString("O")));
    }
}
