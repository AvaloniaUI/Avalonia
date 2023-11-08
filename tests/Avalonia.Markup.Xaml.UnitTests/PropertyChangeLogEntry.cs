namespace Avalonia.Markup.Xaml.UnitTests;

internal class PropertyChangeLogEntry
{
    public PropertyChangeLogEntry(string property, object newValue, object oldValue)
    {
        Property = property;
        NewValue = newValue;
        OldValue = oldValue;
    }

    public string Property { get; }
    public object NewValue { get; }
    public object OldValue { get; }
}
