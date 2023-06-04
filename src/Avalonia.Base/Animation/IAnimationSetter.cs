using Avalonia.Metadata;

namespace Avalonia.Animation
{
    [NotClientImplementable, PrivateApi]
    public interface IAnimationSetter
    {
        AvaloniaProperty? Property { get; set; }
        object? Value { get; set; }
    }
}
