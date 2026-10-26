using System;
using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Automation;

internal static class AutomationPeerAssert
{
    /// <summary>
    /// Asserts that an automation action changes a property without overriding a binding set from a style.
    /// </summary>
    public static void PreservesStyleBinding<T>(
        AvaloniaObject target,
        StyledProperty<T> property,
        T initialValue,
        T automationValue,
        Action automationAction)
    {
        var source = new BindingSource<T>(initialValue);
        target.Bind(property, new Binding(nameof(BindingSource<T>.Value))
        {
            Source = source,
            Priority = BindingPriority.Style
        });
        Assert.Equal(initialValue, target.GetValue(property));

        automationAction();
        Assert.Equal(automationValue, target.GetValue(property));

        source.Value = automationValue;
        source.Value = initialValue;
        Assert.Equal(initialValue, target.GetValue(property));
    }

    private sealed class BindingSource<T>(T value) : NotifyingBase
    {
        private T _value = value;

        public T Value
        {
            get => _value;
            set => SetField(ref _value, value);
        }
    }
}
