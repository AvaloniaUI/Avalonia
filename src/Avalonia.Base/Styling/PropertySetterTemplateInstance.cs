using System;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Utilities;

namespace Avalonia.Styling
{
    internal class PropertySetterTemplateInstance : IValueEntry, ISetterInstance
    {
        private readonly ITemplate _template;
        private readonly StyledElement _target;
        private object? _value;
        private PooledInlineList<StyledElement> _ownedTemplateChildren;
        private bool _isUnsubscribing;
        private bool _isBuilt;

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
            if (!_isBuilt)
            {
                _value = _template.Build();
                _isBuilt = true;
            }

            if (_value is StyledElement element && _ownedTemplateChildren.Count == 0 && !_isUnsubscribing)
                StyledElement.ApplyTemplatedParent(element, _target, ref _ownedTemplateChildren);

            return _value;
        }

        bool IValueEntry.GetDataValidationState(out BindingValueType state, out Exception? error)
        {
            state = BindingValueType.Value;
            error = null;
            return false;
        }

        void IValueEntry.Unsubscribe()
        {
            if (_ownedTemplateChildren.Count == 0)
                return;

            var children = _ownedTemplateChildren;
            _ownedTemplateChildren = default;
            // Reentrant reads must not reattach bindings while this entry is being removed.
            _isUnsubscribing = true;
            try
            {
                foreach (var child in children)
                {
                    if (ReferenceEquals(child.TemplatedParent, _target))
                        child.TemplatedParent = null;
                }
            }
            finally
            {
                _isUnsubscribing = false;
                children.Dispose();
            }
        }
    }
}
