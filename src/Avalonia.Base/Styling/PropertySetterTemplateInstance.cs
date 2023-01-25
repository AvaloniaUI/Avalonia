using System;
using Avalonia.PropertyStore;

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

        public object? GetValue() => _value ??= _template.Build();

        void IValueEntry.Unsubscribe() { }
    }
}
