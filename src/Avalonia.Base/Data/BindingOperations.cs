using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Input;
using Avalonia.Reactive;

namespace Avalonia.Data
{
    public static class BindingOperations
    {
        public static readonly object DoNothing = new DoNothingType();

        /// <summary>
        /// Applies an <see cref="InstancedBinding"/> a property on an <see cref="IAvaloniaObject"/>.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The property to bind.</param>
        /// <param name="binding">The instanced binding.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provide this context.
        /// </param>
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        public static IDisposable Apply(
            IAvaloniaObject target,
            AvaloniaProperty property,
            InstancedBinding binding,
            object? anchor)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            _ = property ?? throw new ArgumentNullException(nameof(property));
            _ = binding ?? throw new ArgumentNullException(nameof(binding));

            var mode = binding.Mode;

            AvaloniaPropertyMetadata? metadata = null;

            if (mode == BindingMode.Default)
            {
                metadata = property.GetMetadata(target.GetType());

                mode = metadata.DefaultBindingMode;
            }

            var updateSourceTrigger = binding.UpdateSourceTrigger;

            if (updateSourceTrigger == UpdateSourceTrigger.Default)
            {
                metadata ??= property.GetMetadata(target.GetType());

                updateSourceTrigger = metadata.DefaultUpdateSourceTrigger;
            }

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    if (binding.Observable is null)
                        throw new InvalidOperationException("InstancedBinding does not contain an observable.");
                    return target.Bind(property, binding.Observable, binding.Priority);

                case BindingMode.TwoWay:
                    if (binding.Subject is null)
                        throw new InvalidOperationException("InstancedBinding does not contain a subject.");

                    var targetPropertyObservable = new TargetPropertyObservable(
                        target,
                        property,
                        updateSourceTrigger);

                    return new TwoWayBindingDisposable(
                        target.Bind(
                            property,
                            binding.Subject,
                            binding.Priority),
                        targetPropertyObservable.Subscribe(binding.Subject));

                case BindingMode.OneTime:
                    var source = binding.Subject ?? binding.Observable;

                    if (source != null)
                    {
                        // Perf: Avoid allocating closure in the outer scope.
                        var targetCopy = target;
                        var propertyCopy = property;
                        var bindingCopy = binding;

                        return source
                            .Where(x => BindingNotification.ExtractValue(x) != AvaloniaProperty.UnsetValue)
                            .Take(1)
                            .Subscribe(x => targetCopy.SetValue(
                                propertyCopy,
                                BindingNotification.ExtractValue(x),
                                bindingCopy.Priority));
                    }

                    target.SetValue(property, binding.Value, binding.Priority);

                    return Disposable.Empty;

                case BindingMode.OneWayToSource:
                {
                    if (binding.Observable is null)
                        throw new InvalidOperationException("InstancedBinding does not contain an observable.");
                    if (binding.Subject is null)
                        throw new InvalidOperationException("InstancedBinding does not contain a subject.");

                    // Perf: Avoid allocating closure in the outer scope.
                    var bindingCopy = binding;

                    return Observable.CombineLatest(
                            binding.Observable,
                            new TargetPropertyObservable(
                                target,
                                property,
                                updateSourceTrigger),
                            (_, v) => v)
                        .Subscribe(x => bindingCopy.Subject.OnNext(x));
                }

                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        private sealed class TargetPropertyObservable : SingleSubscriberObservableBase<object?>
        {
            private readonly WeakReference<IAvaloniaObject> _target;
            private readonly AvaloniaProperty _property;
            private readonly UpdateSourceTrigger _updateSourceTrigger;

            public TargetPropertyObservable(
                IAvaloniaObject target,
                AvaloniaProperty property,
                UpdateSourceTrigger updateSourceTrigger)
            {
                _target = new WeakReference<IAvaloniaObject>(target);
                _property = property;
                _updateSourceTrigger = updateSourceTrigger;
            }

            private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                switch (_updateSourceTrigger)
                {
                    case UpdateSourceTrigger.Default or UpdateSourceTrigger.PropertyChanged
                        when e.Property == _property:

                        PublishNext(e.NewValue);

                        break;

                    case UpdateSourceTrigger.LostFocus when
                        e.Property == InputElement.IsFocusedProperty &&
                        e.OldValue is true &&
                        e.NewValue is false:

                        PublishNext();
                        break;
                }
            }

            private void PublishNext()
            {
                if (_target.TryGetTarget(out var target))
                {
                    var value = target.GetValue(_property);

                    PublishNext(value);
                }
            }

            protected override void Unsubscribed()
            {
                if (_target.TryGetTarget(out var target))
                {
                    target.PropertyChanged -= OnTargetPropertyChanged;
                }
            }

            protected override void Subscribed()
            {
                if (_updateSourceTrigger is
                    UpdateSourceTrigger.Default or
                    UpdateSourceTrigger.PropertyChanged or
                    UpdateSourceTrigger.LostFocus)
                {
                    if (_target.TryGetTarget(out var target))
                    {
                        target.PropertyChanged += OnTargetPropertyChanged;
                    }

                    if (_updateSourceTrigger is UpdateSourceTrigger.Default or UpdateSourceTrigger.PropertyChanged)
                    {
                        PublishNext();
                    }
                }
            }
        }

        private sealed class TwoWayBindingDisposable : IDisposable
        {
            private readonly IDisposable _toTargetSubscription;
            private readonly IDisposable _fromTargetSubsription;

            private bool _isDisposed;

            public TwoWayBindingDisposable(IDisposable toTargetSubscription, IDisposable fromTargetSubsription)
            {
                _toTargetSubscription = toTargetSubscription;
                _fromTargetSubsription = fromTargetSubsription;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _fromTargetSubsription.Dispose();
                _toTargetSubscription.Dispose();

                _isDisposed = true;
            }
        }
    }

    public sealed class DoNothingType
    {
        internal DoNothingType() { }

        /// <summary>
        /// Returns the string representation of <see cref="BindingOperations.DoNothing"/>.
        /// </summary>
        /// <returns>The string "(do nothing)".</returns>
        public override string ToString() => "(do nothing)";
    }
}
