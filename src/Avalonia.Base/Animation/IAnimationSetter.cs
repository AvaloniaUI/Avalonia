using Avalonia.Metadata;

namespace Avalonia.Animation
{
    [NotClientImplementable]
    public interface IAnimationSetter
    {
        AvaloniaProperty? Property { get; set; }
        object? Value { get; set; }
    }
}
