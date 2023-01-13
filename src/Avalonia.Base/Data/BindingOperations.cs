using System;
using Avalonia.Reactive;

namespace Avalonia.Data
{
    public static class BindingOperations
    {
        public static readonly object DoNothing = new DoNothingType();

        /// <summary>
        /// Applies an <see cref="InstancedBinding"/> a property on an <see cref="AvaloniaObject"/>.
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
            AvaloniaObject target,
            AvaloniaProperty property,
            InstancedBinding binding,
            object? anchor)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            _ = property ?? throw new ArgumentNullException(nameof(property));
            _ = binding ?? throw new ArgumentNullException(nameof(binding));

            var mode = binding.Mode;

            if (mode == BindingMode.Default)
            {
                mode = property.GetMetadata(target.GetType()).DefaultBindingMode;
            }

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    if (binding.Observable is null)
                        throw new InvalidOperationException("InstancedBinding does not contain an observable.");
                    return target.Bind(property, binding.Observable, binding.Priority);
                case BindingMode.TwoWay:
                    if (binding.Observable is null)
                        throw new InvalidOperationException("InstancedBinding does not contain an observable.");
                    if (binding.Subject is null)
                        throw new InvalidOperationException("InstancedBinding does not contain a subject.");
                    return new TwoWayBindingDisposable(
                        target.Bind(property, binding.Observable, binding.Priority),
                        target.GetObservable(property).Subscribe(binding.Subject));
                case BindingMode.OneTime:
                    if (binding.Observable is {} source)
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
                        target.GetObservable(property),
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
