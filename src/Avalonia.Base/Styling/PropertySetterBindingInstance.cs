using System;
using Avalonia.Data;
using Avalonia.PropertyStore;

namespace Avalonia.Styling
{
    internal class PropertySetterBindingInstance : UntypedBindingEntry, ISetterInstance
    {
        private readonly AvaloniaObject _target;
        private readonly BindingMode _mode;

        public PropertySetterBindingInstance(
            AvaloniaObject target,
            StyleInstance instance,
            AvaloniaProperty property,
            BindingMode mode,
            IObservable<object?> source)
            : base(target, instance, property, source)
        {
            _target = target;
            _mode = mode;

            if (mode == BindingMode.TwoWay &&
                source is not IObserver<object?>)
            {
                throw new NotSupportedException(
                    "Attempting to bind two-way with a binding source which doesn't support it.");
            }
        }

        public override void Unsubscribe()
        {
            _target.PropertyChanged -= PropertyChanged;
            base.Unsubscribe();
        }

        protected override void Start(bool produceValue)
        {
            if (!IsSubscribed)
            {
                if (_mode == BindingMode.TwoWay)
                {
                    var observer = (IObserver<object?>)Source;
                    _target.PropertyChanged += PropertyChanged;
                }

                base.Start(produceValue);
            }
        }

        private void PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Property && e.Priority >= BindingPriority.LocalValue)
            {
                if (Frame.Owner is not null && !Frame.Owner.IsEvaluating)
                    ((IObserver<object?>)Source).OnNext(e.NewValue);
            }
        }
    }
}
