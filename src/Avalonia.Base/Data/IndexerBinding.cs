namespace Avalonia.Data
{
    public class IndexerBinding : IBinding
    {
        public IndexerBinding(
            IAvaloniaObject source,
            AvaloniaProperty property,
            BindingMode mode)
        {
            Source = source;
            Property = property;
            Mode = mode;
        }

        private IAvaloniaObject Source { get; }
        public AvaloniaProperty Property { get; }
        private BindingMode Mode { get; }

        public InstancedBinding? Initiate(
            IAvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            return new InstancedBinding(Source.GetSubject(Property), Mode, BindingPriority.LocalValue);
        }
    }
}
