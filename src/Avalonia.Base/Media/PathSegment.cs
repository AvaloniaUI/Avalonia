namespace Avalonia.Media
{
    public abstract class PathSegment : AvaloniaObject
    {
        internal abstract void ApplyTo(StreamGeometryContext ctx);

        public static readonly StyledProperty<bool> IsStrokedProperty =
            AvaloniaProperty.Register<PathSegment, bool>(nameof(IsStroked), true);

        public bool IsStroked
        {
            get => GetValue(IsStrokedProperty);
            set => SetValue(IsStrokedProperty, value);
        }
    }
}
