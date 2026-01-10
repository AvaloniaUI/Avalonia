using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers;

public class DatePickerAutomationPeer : ControlAutomationPeer, IValueProvider
{
    public DatePickerAutomationPeer(DatePicker owner)
        : base(owner)
    {
    }

    public bool IsReadOnly => false;
    public new DatePicker Owner => (DatePicker)base.Owner;
    public string? Value => Owner.SelectedDate?.ToString();

    public void SetValue(string? value)
    {
        if (DateTimeOffset.TryParse(value, out var result))
            Owner.SelectedDate = result;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
}
