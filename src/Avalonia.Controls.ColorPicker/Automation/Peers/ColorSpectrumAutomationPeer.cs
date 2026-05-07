using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Avalonia.Automation.Peers;

public class ColorSpectrumAutomationPeer : ControlAutomationPeer, IValueProvider
{
    public ColorSpectrumAutomationPeer(ColorSpectrum owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    public bool IsReadOnly => false;
    public new ColorSpectrum Owner => (ColorSpectrum)base.Owner;
    public string? Value => Owner.Color.ToString();

    public void SetValue(string? value)
    {
        if (!Color.TryParse(value, out var color))
        {
            throw new System.FormatException($"Invalid color string: '{value}'.");
        }

        Owner.Color = color;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

    protected override string GetClassNameCore() => nameof(ColorSpectrum);

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ColorSpectrum.ColorProperty)
        {
            RaisePropertyChangedEvent(ValuePatternIdentifiers.ValueProperty, e.OldValue?.ToString(), e.NewValue?.ToString());
        }
    }
}
