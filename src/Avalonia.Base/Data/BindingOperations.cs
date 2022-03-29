using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;

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

            static IObservable<object?> GetTargetPropertyObservable(
                InstancedBinding binding,
                IAvaloniaObject target,
                AvaloniaProperty property,
                UpdateSourceTrigger updateSourceTrigger)
            {
                var explicitSourceUpdate = binding
                    .ExplicitUpdateRequested
                    .Where(m => m == InstancedBinding.ExplicitUpdateMode.Source)
                    .Select(_ => target.GetValue(property));

                IObservable<object?> valueObservable;

                switch (updateSourceTrigger)
                {
                    case UpdateSourceTrigger.PropertyChanged:
                    case UpdateSourceTrigger.Default:

                        valueObservable = target.GetObservable(property);

                        break;


                    case UpdateSourceTrigger.Explicit:

                        return explicitSourceUpdate;


                    case UpdateSourceTrigger.LostFocus:

                        valueObservable = Observable.FromEventPattern<AvaloniaPropertyChangedEventArgs>(
                                x => target.PropertyChanged += x,
                                x => target.PropertyChanged -= x)
                            .Where(e => e.EventArgs.Property.Name.Equals("IsFocused") &&
                                        e.EventArgs.OldValue is true &&
                                        e.EventArgs.NewValue is false)
                            .Select(_ => target.GetValue(property));

                        break;


                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(updateSourceTrigger),
                            updateSourceTrigger,
                            null);
                }

                return valueObservable.Merge(explicitSourceUpdate);
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

                    return new TwoWayBindingDisposable(
                        target.Bind(
                            property, 
                            GetSourceObservable(binding), 
                            binding.Priority),
                        GetTargetPropertyObservable(
                            binding,
                            target,
                            property,
                            updateSourceTrigger).Subscribe(binding.Subject));

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
                            GetTargetPropertyObservable(
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
