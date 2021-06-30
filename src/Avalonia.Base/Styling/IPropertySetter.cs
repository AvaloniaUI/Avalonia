namespace Avalonia.Styling
{
    public interface IPropertySetter
    {
        AvaloniaProperty Property { get; }

        object Value { get; }
    }
}
