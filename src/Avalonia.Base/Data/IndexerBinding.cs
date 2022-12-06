namespace Avalonia.Data
{
    public class IndexerBinding : IBinding
    {
        public IndexerBinding(
            AvaloniaObject source,
            AvaloniaProperty property,
            BindingMode mode)
        {
            Source = source;
            Property = property;
            Mode = mode;
        }

        private AvaloniaObject Source { get; }
        public AvaloniaProperty Property { get; }
        private BindingMode Mode { get; }

        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            return new InstancedBinding(Source.GetSubject(Property), Mode, BindingPriority.LocalValue);
        }
    }
}
