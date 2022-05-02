using System;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

                updateSourceTrigger = metadata.UpdateSourceTrigger;
            }

            static IObservable<object?> GetSourceObservable(InstancedBinding binding)
            {
                var explicitTargetUpdate = binding
                    .ExplicitUpdateRequested
                    .Where(m => m == InstancedBinding.ExplicitUpdateMode.Target);

                Debug.Assert(binding.Observable != null);

                return binding.Observable.CombineLatest(explicitTargetUpdate, (v, _) => v);
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
                            binding,
                            target, 
                            property, 
                            updateSourceTrigger);

                    return new TwoWayBindingDisposable(
                        target.Bind(
                            property, 
                            GetSourceObservable(binding), 
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
                    else
                    {
                        target.SetValue(property, binding.Value, binding.Priority);
                        return Disposable.Empty;
                    }

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
                                binding,
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

        private sealed class TargetPropertyObservable : LightweightObservableBase<object?>
        {
            private readonly InstancedBinding _binding;
            private readonly WeakReference<IAvaloniaObject> _target;
            private readonly AvaloniaProperty _property;
            private readonly UpdateSourceTrigger _updateSourceTrigger;
            private IDisposable? _explicitUpdateSubscription;

            public TargetPropertyObservable(
                InstancedBinding binding,
                IAvaloniaObject target,
                AvaloniaProperty property,
                UpdateSourceTrigger updateSourceTrigger)
            {
                _binding = binding;
                _target = new WeakReference<IAvaloniaObject>(target);
                _property = property;
                _updateSourceTrigger = updateSourceTrigger;
            }

            private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (_updateSourceTrigger == UpdateSourceTrigger.LostFocus &&
                    e.Property == InputElement.IsFocusedProperty &&
                    e.OldValue is true &&
                    e.NewValue is false)
                {
                    PublishNext();
                }
                else if (e.Property == _property)
                {
                    PublishNext(e.NewValue);
                }
            }

            private void OnExplicitUpdateRequested(InstancedBinding.ExplicitUpdateMode updateMode)
            {
                if (updateMode == InstancedBinding.ExplicitUpdateMode.Source)
                {
                    PublishNext();
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

            protected override void Initialize()
            {
                _explicitUpdateSubscription =
                    _binding.ExplicitUpdateRequested.Subscribe(OnExplicitUpdateRequested);

                if (_updateSourceTrigger is 
                    UpdateSourceTrigger.Default or 
                    UpdateSourceTrigger.PropertyChanged or 
                    UpdateSourceTrigger.LostFocus)
                {
                    if (_target.TryGetTarget(out var target))
                    {
                        target.PropertyChanged += OnTargetPropertyChanged;
                    }
                }
            }

            protected override void Deinitialize()
            {
                if (_target.TryGetTarget(out var target))
                {
                    target.PropertyChanged -= OnTargetPropertyChanged;
                }

                _explicitUpdateSubscription?.Dispose();
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
