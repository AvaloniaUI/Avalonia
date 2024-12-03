using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class TimePickerAutomationPeer : ControlAutomationPeer, IValueProvider
{
    public TimePickerAutomationPeer(TimePicker owner)
        : base(owner)
    {
    }

    public bool IsReadOnly => false;
    public new TimePicker Owner => (TimePicker)base.Owner;
    public string? Value => Owner.SelectedTime?.ToString();

    public void SetValue(string? value)
    {
        if (TimeSpan.TryParse(value, out var result))
            Owner.SelectedTime = result;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
