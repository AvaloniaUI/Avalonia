using Avalonia.Animation.Easings;

namespace Avalonia.Animation
{
    public class ExpressionKeyFrame : AvaloniaObject
    {
        public float NormalizedProgressKey { get; set; }
        public string? Value { get; set; }
        public IEasing? Easing { get; set; }
    }
}
