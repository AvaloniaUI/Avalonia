using System;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Styling;

#nullable enable

namespace Avalonia.Controls
{
    public static class ResourceNodeExtensions
    {
        /// <summary>
        /// Finds the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or <see cref="AvaloniaProperty.UnsetValue"/> if not found.</returns>
        public static object? FindResource(this IResourceHost control, object key)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            if (control.TryFindResourceWithParentCheck(key, out var value).found)
            {
                return value;
            }

            return AvaloniaProperty.UnsetValue;
        }

        /// <summary>
        /// Tries to the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <param name="value">On return, contains the resource if found, otherwise null.</param>
        /// <returns>True if the resource was found; otherwise false.</returns>
        public static bool TryFindResource(this IResourceHost control, object key, out object? value)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return control.TryFindResourceWithParentCheck(key, out value).found;
        }

        public static IObservable<object?> GetResourceObservable(
            this IStyledElement control,
            object key,
            Func<object?, object?>? converter = null)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new ResourceObservable(control, key, converter);
        }

        public static IObservable<object?> GetResourceObservable(
            this IResourceProvider resourceProvider,
            object key,
            Func<object?, object?>? converter = null)
        {
            resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new FloatingResourceObservable(resourceProvider, key, converter);
        }

        private static (bool found, bool? hostAttachedToStylingTree) TryFindResourceWithParentCheck(this IResourceHost control, object key, out object? value)
        {
            IResourceHost? current = control;
            IResourceHost? last = current;

            while (current != null)
            {
                last = current;

                if (current is IResourceHost host)
                {
                    if (host.TryGetResource(key, out value))
                    {
                        // We return null because there is no enough information on current loop iteration.
                        return (true, last is IGlobalStyles ? true : (bool?)null);
                    }
                }

                current = (current as IStyledElement)?.StylingParent as IResourceHost;
            }

            value = null;

            // IGlobalStyles is styling tree root node.
            return (false, last is IGlobalStyles);
        }

        private class ResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IStyledElement _target;
            private readonly object _key;
            private readonly Func<object?, object?>? _converter;

            public ResourceObservable(IStyledElement target, object key, Func<object?, object?>? converter)
            {
                _target = target;
                _key = key;
                _converter = converter;
            }

            protected override void Initialize()
            {
                _target.ResourcesChanged += ResourcesChanged;
            }

            protected override void Deinitialize()
            {
                _target.ResourcesChanged -= ResourcesChanged;
            }

            protected override void Subscribed(IObserver<object?> observer, bool first)
            {
                var (found, hostAttachedToStylingTree) = _target.TryFindResourceWithParentCheck(_key, out var value);
                if (!found)
                {
                    value = AvaloniaProperty.UnsetValue;

                    if (hostAttachedToStylingTree == true)
                    {
                        Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(this, "Warning: Dynamic resource '{Key}' was not found in the {Target}.", _key, _target.GetType().Name);
                    }
                }

                observer.OnNext(Convert(value));
            }

            private void ResourcesChanged(object sender, ResourcesChangedEventArgs e)
            {
                var (found, hostAttachedToStylingTree) = _target.TryFindResourceWithParentCheck(_key, out var value);
                if (!found)
                {
                    value = AvaloniaProperty.UnsetValue;

                    if (hostAttachedToStylingTree == true)
                    {
                        Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(this, "Warning: Dynamic resource '{Key}' was not found in the {Target}.", _key, _target.GetType().Name);
                    }
                }

                PublishNext(Convert(value));
            }

            private object? Convert(object? value) => _converter?.Invoke(value) ?? value;
        }

        private class FloatingResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IResourceProvider _target;
            private readonly object _key;
            private readonly Func<object?, object?>? _converter;
            private IResourceHost? _owner;

            public FloatingResourceObservable(IResourceProvider target, object key, Func<object?, object?>? converter)
            {
                _target = target;
                _key = key;
                _converter = converter;
            }

            protected override void Initialize()
            {
                _target.OwnerChanged += OwnerChanged;
                _owner = _target.Owner;
            }

            protected override void Deinitialize()
            {
                _target.OwnerChanged -= OwnerChanged;
                _owner = null;
            }

            protected override void Subscribed(IObserver<object?> observer, bool first)
            {
                if (_target.Owner is IResourceHost owner)
                {
                    var (found, hostAttachedToStylingTree) = owner.TryFindResourceWithParentCheck(_key, out var value);
                    if (!found)
                    {
                        value = AvaloniaProperty.UnsetValue;

                        if (hostAttachedToStylingTree == true)
                        {
                            Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(this, "Warning: Dynamic resource '{Key}' was not found in the {Owner}.", _key, owner.GetType().Name);
                        }
                    }

                    observer.OnNext(Convert(value));
                }
            }

            private void PublishNext()
            {
                if (_target.Owner is IResourceHost owner)
                {
                    var (found, hostAttachedToStylingTree) = owner.TryFindResourceWithParentCheck(_key, out var value);
                    if (!found)
                    {
                        value = AvaloniaProperty.UnsetValue;

                        if (hostAttachedToStylingTree == true)
                        {
                            Logger.TryGet(LogEventLevel.Warning, LogArea.Binding)?.Log(this, "Warning: Dynamic resource '{Key}' was not found in the {Owner}.", _key, owner.GetType().Name);
                        }
                    }

                    PublishNext(Convert(value));
                }
                else
                {
                    PublishNext(Convert(null));
                }
            }

            private void OwnerChanged(object sender, EventArgs e)
            {
                if (_owner is object)
                {
                    _owner.ResourcesChanged -= ResourcesChanged;
                }

                _owner = _target.Owner;

                if (_owner is object)
                {
                    _owner.ResourcesChanged += ResourcesChanged;
                }

                PublishNext();
            }

            private void ResourcesChanged(object sender, ResourcesChangedEventArgs e)
            {
                PublishNext();
            }

            private object? Convert(object? value) => _converter?.Invoke(value) ?? value;
        }
    }
}
