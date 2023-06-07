using Avalonia.Reactive;

namespace Avalonia.Data
{
    internal class IndexerBinding : IBinding
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
            var subject = new CombinedSubject<object?>(
                new AnonymousObserver<object?>(x => Source.SetValue(Property, x, BindingPriority.LocalValue)),
                Source.GetObservable(Property));
            return new InstancedBinding(subject, Mode, BindingPriority.LocalValue);
        }
    }
}
