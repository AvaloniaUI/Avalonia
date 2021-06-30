using Avalonia.PropertyStore;

#nullable enable

namespace Avalonia.Styling
{
    internal class PropertySetterTemplateInstance : IValueEntry, ISetterInstance
    {
        private readonly ITemplate _template;
        private object? _value;

        public PropertySetterTemplateInstance(AvaloniaProperty property, ITemplate template)
        {
            _template = template;
            Property = property;
        }

        public bool HasValue => true;
        public AvaloniaProperty Property { get; }

        public bool TryGetValue(out object? value)
        {
            value = _value ??= _template.Build();
            return value != AvaloniaProperty.UnsetValue;
        }
    }
}
