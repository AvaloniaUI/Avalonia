using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
            object anchor)
        {
            Contract.Requires<ArgumentNullException>(target != null);
            Contract.Requires<ArgumentNullException>(property != null);
            Contract.Requires<ArgumentNullException>(binding != null);

            var mode = binding.Mode;

            if (mode == BindingMode.Default)
            {
                mode = property.GetMetadata(target.GetType()).DefaultBindingMode;
            }

            switch (mode)
            {
                case BindingMode.Default:
                case BindingMode.OneWay:
                    return target.Bind(property, binding.Observable ?? binding.Subject, binding.Priority);
                case BindingMode.TwoWay:
                    return new TwoWayBindingDisposable(
                        target.Bind(property, binding.Subject, binding.Priority),
                        target.GetObservable(property).Subscribe(binding.Subject));
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
            private readonly IDisposable _first;
            private readonly IDisposable _second;
            private bool _isDisposed;

            public TwoWayBindingDisposable(IDisposable first, IDisposable second)
            {
                _first = first;
                _second = second;
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _first.Dispose();
                _second.Dispose();

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
