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
        private WeakReference<StyledElement>[]? _ownedTemplateChildren;
        private bool _isUnsubscribing;
        private bool _isBuilt;
        private bool _isActive;

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
                if (_value is StyledElement element)
                {
                    var children = new PooledInlineList<StyledElement>();
                    try
                    {
                        StyledElement.ApplyTemplatedParent(element, _target, ref children);
                        _ownedTemplateChildren = new WeakReference<StyledElement>[children.Count];
                        var i = 0;
                        foreach (var child in children)
                        {
                            child.IsSetterTemplateChild = true;
                            _ownedTemplateChildren[i++] = new(child);
                        }
                    }
                    finally
                    {
                        children.Dispose();
                    }
                }
                _isActive = true;
            }

            // Reuse only the original identities: bindings may have since forwarded user
            // content into the subtree. Detached descendants must remain collectible.
            if (!_isActive && !_isUnsubscribing && _ownedTemplateChildren is { } owned)
            {
                _isActive = true;
                foreach (var reference in owned)
                {
                    if (reference.TryGetTarget(out var child) && child.TemplatedParent is null)
                    {
                        child.TemplatedParent = _target;
                        child.IsSetterTemplateChild = true;
                    }
                }
            }

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
            if (!_isActive || _ownedTemplateChildren is not { } children)
                return;

            _isActive = false;
            // Reentrant reads must not reattach bindings while this entry is being removed.
            _isUnsubscribing = true;
            try
            {
                foreach (var reference in children)
                {
                    if (reference.TryGetTarget(out var child) && ReferenceEquals(child.TemplatedParent, _target))
                        child.TemplatedParent = null;
                }
            }
            finally
            {
                _isUnsubscribing = false;
            }
        }
    }
}
