using System;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    internal class PropertySetterTemplateInstance : IValueEntry, ISetterInstance
    {
        private readonly ITemplate _template;
        private readonly StyledElement _target;
        private object? _value;

        public PropertySetterTemplateInstance(AvaloniaProperty property, ITemplate template, StyledElement target)
        {
            _template = template;
            _target = target;
            Property = property;
        }

        public AvaloniaProperty Property { get; }

        public bool HasValue() => true;
        public object? GetValue()
        {
            if (_value is null)
            {
                _value = _template.Build();

                if (_value is StyledElement element)
                    StyledElement.ApplyTemplatedParent(element, _target);
            }

            return _value;
        }

        bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            state = BindingValueType.Value;
            error = null;
            return false;
        }

        void IValueEntry.Unsubscribe() { }
    }
}
