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
        // The root is already held by _value. Descendants are stored as null, a single
        // weak reference, or an array of weak references, depending on their count.
        private object? _ownedTemplateChildren;
        private bool _isUnsubscribing;
        private bool _isInitializing;
        private bool _unsubscribeRequestedDuringInitialization;
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
            if (_isInitializing)
                _unsubscribeRequestedDuringInitialization = false;

            if (!_isBuilt)
            {
                _value = _template.Build();
                _isBuilt = true;
                if (_value is StyledElement element)
                {
                    var children = new PooledInlineList<StyledElement>();
                    _isInitializing = true;
                    try
                    {
                        StyledElement.ApplyTemplatedParent(element, _target, ref children);
                        var enumerator = children.GetEnumerator();
                        if (ReferenceEquals(element.TemplatedParent, _target))
                            element.IsSetterTemplateChild = true;

                        if (children.Count == 1)
                        {
                            enumerator.MoveNext();
                            var child = enumerator.Current;
                            if (ReferenceEquals(child.TemplatedParent, _target))
                                child.IsSetterTemplateChild = true;
                            _ownedTemplateChildren = new WeakReference<StyledElement>(child);
                        }
                        else if (children.Count > 1)
                        {
                            var owned = new WeakReference<StyledElement>[children.Count];
                            var i = 0;
                            while (enumerator.MoveNext())
                            {
                                var child = enumerator.Current;
                                if (ReferenceEquals(child.TemplatedParent, _target))
                                    child.IsSetterTemplateChild = true;
                                owned[i++] = new(child);
                            }
                            _ownedTemplateChildren = owned;
                        }
                    }
                    finally
                    {
                        children.Dispose();
                        _isInitializing = false;
                    }
                }
                _isActive = true;

                if (_unsubscribeRequestedDuringInitialization)
                {
                    _unsubscribeRequestedDuringInitialization = false;
                    ((IValueEntry)this).Unsubscribe();
                    return _value;
                }
            }

            // Reuse only the original identities: bindings may have since forwarded user
            // content into the subtree. Detached descendants must remain collectible.
            if (!_isActive && !_isUnsubscribing && !_isInitializing &&
                _value is StyledElement elementToRestore)
            {
                _isActive = true;
                RestoreTemplatedParent(elementToRestore);

                if (!_isActive)
                    return _value;

                if (_ownedTemplateChildren is WeakReference<StyledElement> single)
                {
                    if (single.TryGetTarget(out var child))
                        RestoreTemplatedParent(child);
                }
                else if (_ownedTemplateChildren is WeakReference<StyledElement>[] owned)
                {
                    foreach (var reference in owned)
                    {
                        if (!_isActive)
                            break;

                        if (reference.TryGetTarget(out var child))
                            RestoreTemplatedParent(child);
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
            if (_isInitializing)
            {
                _unsubscribeRequestedDuringInitialization = true;
                return;
            }

            if (!_isActive || _value is not StyledElement element)
                return;

            _isActive = false;
            // Reentrant reads must not reattach bindings while this entry is being removed.
            _isUnsubscribing = true;
            try
            {
                ClearTemplatedParent(element);

                if (_ownedTemplateChildren is WeakReference<StyledElement> single)
                {
                    if (single.TryGetTarget(out var child))
                        ClearTemplatedParent(child);
                }
                else if (_ownedTemplateChildren is WeakReference<StyledElement>[] children)
                {
                    foreach (var reference in children)
                    {
                        if (reference.TryGetTarget(out var child))
                            ClearTemplatedParent(child);
                    }
                }
            }
            finally
            {
                _isUnsubscribing = false;
            }
        }

        /// <summary>
        /// Restores an original template child's ownership unless it has been reassigned.
        /// </summary>
        private void RestoreTemplatedParent(StyledElement child)
        {
            if (_isActive && child.TemplatedParent is null)
            {
                child.TemplatedParent = _target;

                if (_isActive && ReferenceEquals(child.TemplatedParent, _target))
                    child.IsSetterTemplateChild = true;
            }
        }

        /// <summary>
        /// Clears ownership only while the child still belongs to this setter's target.
        /// </summary>
        private void ClearTemplatedParent(StyledElement child)
        {
            if (ReferenceEquals(child.TemplatedParent, _target))
                child.TemplatedParent = null;
        }
    }
}
