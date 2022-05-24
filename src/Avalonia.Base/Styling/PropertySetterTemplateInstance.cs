using System;
using Avalonia.Data;
using Avalonia.Reactive;

#nullable enable

namespace Avalonia.Styling
{
    /// <summary>
    /// A <see cref="Setter"/> which has been instanced on a control and whose value is lazily
    /// evaluated.
    /// </summary>
    /// <typeparam name="T">The target property type.</typeparam>
    internal class PropertySetterTemplateInstance<T> : SingleSubscriberObservableBase<BindingValue<T>>,
        ISetterInstance
    {
        private readonly IStyleable _target;
        private readonly StyledPropertyBase<T>? _styledProperty;
        private readonly DirectPropertyBase<T>? _directProperty;
        private readonly ITemplate _template;
        private BindingValue<T> _value;
        private IDisposable? _subscription;
        private bool _isActive;

        public PropertySetterTemplateInstance(
            IStyleable target,
            StyledPropertyBase<T> property,
            ITemplate template)
        {
            _target = target;
            _styledProperty = property;
            _template = template;
        }

        public PropertySetterTemplateInstance(
            IStyleable target,
            DirectPropertyBase<T> property,
            ITemplate template)
        {
            _target = target;
            _directProperty = property;
            _template = template;
        }

        public void Start(bool hasActivator)
        {
            _isActive = !hasActivator;

            if (_styledProperty is not null)
            {
                var priority = hasActivator ? BindingPriority.StyleTrigger : BindingPriority.Style;
                _subscription = _target.Bind(_styledProperty, this, priority);
            }
            else
            {
                _subscription = _target.Bind(_directProperty!, this);
            }
        }

        public void Activate()
        {
            if (!_isActive)
            {
                _isActive = true;
                PublishNext();
            }
        }

        public void Deactivate()
        {
            if (_isActive)
            {
                _isActive = false;
                PublishNext();
            }
        }

        public override void Dispose()
        {
            if (_subscription is not null)
            {
                var sub = _subscription;
                _subscription = null;
                sub.Dispose();
            }
            else if (_isActive)
            {
                if (_styledProperty is not null)
                {
                    _target.ClearValue(_styledProperty);
                }
                else
                {
                    _target.ClearValue(_directProperty!);
                }
            }

            base.Dispose();
        }

        protected override void Subscribed() => PublishNext();
        protected override void Unsubscribed() { }

        private void EnsureTemplate()
        {
            if (_value.HasValue)
            {
                return;
            }

            _value = (T) _template.Build();
        }

        private void PublishNext()
        {
            if (_isActive)
            {
                EnsureTemplate();
                PublishNext(_value);
            }
            else
            {
                PublishNext(default);
            }
        }
    }
}
