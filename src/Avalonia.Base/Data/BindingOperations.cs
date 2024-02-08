using System;
using Avalonia.Diagnostics;
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
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        [Obsolete(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        public static IDisposable Apply(
            AvaloniaObject target,
            AvaloniaProperty property,
            InstancedBinding binding)
        {
            _ = target ?? throw new ArgumentNullException(nameof(target));
            _ = property ?? throw new ArgumentNullException(nameof(property));
            _ = binding ?? throw new ArgumentNullException(nameof(binding));

            if (binding.Expression is { } expression)
            {
                return target.GetValueStore().AddBinding(property, expression);
            }

            var mode = binding.Mode;

            if (mode == BindingMode.Default)
            {
                mode = property.GetMetadata(target.GetType()).DefaultBindingMode;
            }

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, binding.Source, binding.Priority);
                case BindingMode.TwoWay:
                {
                    if (binding.Source is not IObserver<object?> observer)
                        throw new InvalidOperationException("InstancedBinding does not contain a subject.");
                    return new TwoWayBindingDisposable(
                        target.Bind(property, binding.Source, binding.Priority),
                        target.GetObservable(property).Subscribe(observer));
                }
                case BindingMode.OneTime:
                {
                    // Perf: Avoid allocating closure in the outer scope.
                    var targetCopy = target;
                    var propertyCopy = property;
                    var bindingCopy = binding;

                    return binding.Source
                        .Where(x => BindingNotification.ExtractValue(x) != AvaloniaProperty.UnsetValue)
                        .Take(1)
                        .Subscribe(x => targetCopy.SetValue(
                            propertyCopy,
                            BindingNotification.ExtractValue(x),
                            bindingCopy.Priority));
                }

                case BindingMode.OneWayToSource:
                {
                    if (binding.Source is not IObserver<object?> observer)
                        throw new InvalidOperationException("InstancedBinding does not contain a subject.");

                    return Observable.CombineLatest(
                        binding.Source,
                        target.GetObservable(property),
                        (_, v) => v)
                    .Subscribe(x => observer.OnNext(x));
                }

                default:
                    throw new ArgumentException("Invalid binding mode.");
            }
        }

        /// <summary>
        /// Applies an <see cref="InstancedBinding"/> a property on an <see cref="AvaloniaObject"/>.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <param name="property">The property to bind.</param>
        /// <param name="binding">The instanced binding.</param>
        /// <param name="anchor">Obsolete, unused.</param>
        /// <returns>An <see cref="IDisposable"/> which can be used to cancel the binding.</returns>
        [Obsolete("Use the Apply(AvaloniaObject, AvaloniaProperty, InstancedBinding) overload.")]
        public static IDisposable Apply(
            AvaloniaObject target,
            AvaloniaProperty property,
            InstancedBinding binding,
            object? anchor)
        {
            return Apply(target, property, binding);
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
