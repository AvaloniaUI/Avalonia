using System;
using Avalonia.LogicalTree;
using Avalonia.Reactive;

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

            if (control.TryFindResource(key, out var value))
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

            IResourceHost? current = control;

            while (current != null)
            {
                if (current is IResourceHost host)
                {
                    if (host.TryGetResource(key, out value))
                    {
                        return true;
                    }
                }

                current = (current as IStyledElement)?.StylingParent as IResourceHost;
            }

            value = null;
            return false;
        }

        public static IObservable<object?> GetResourceObservable(this IStyledElement control, object key)
        {
            control = control ?? throw new ArgumentNullException(nameof(control));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new ResourceObservable(control, key);
        }

        public static IObservable<object?> GetResourceObservable(this IResourceProvider resourceProvider, object key)
        {
            resourceProvider = resourceProvider ?? throw new ArgumentNullException(nameof(resourceProvider));
            key = key ?? throw new ArgumentNullException(nameof(key));

            return new FloatingResourceObservable(resourceProvider, key);
        }

        private class ResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IStyledElement _target;
            private readonly object _key;

            public ResourceObservable(IStyledElement target, object key)
            {
                _target = target;
                _key = key;
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
                observer.OnNext(_target.FindResource(_key));
            }

            private void ResourcesChanged(object sender, ResourcesChangedEventArgs e)
            {
                PublishNext(_target.FindResource(_key));
            }
        }

        private class FloatingResourceObservable : LightweightObservableBase<object?>
        {
            private readonly IResourceProvider _target;
            private readonly object _key;
            private IResourceHost? _owner;

            public FloatingResourceObservable(IResourceProvider target, object key)
            {
                _target = target;
                _key = key;
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
                if (_target.Owner is object)
                {
                    observer.OnNext(_target.Owner?.FindResource(_key));
                }
            }

            private void PublishNext()
            {
                PublishNext(_target.Owner?.FindResource(_key));
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
        }
    }
}
