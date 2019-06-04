using System;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    public static class ResourceProviderExtensions
    {
        /// <summary>
        /// Finds the specified resource by searching up the logical tree and then global styles.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="key">The resource key.</param>
        /// <returns>The resource, or <see cref="AvaloniaProperty.UnsetValue"/> if not found.</returns>
        public static object FindResource(this IResourceNode control, object key)
        {
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
        public static bool TryFindResource(this IResourceNode control, object key, out object value)
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Contract.Requires<ArgumentNullException>(key != null);

            var current = control;

            while (current != null)
            {
                if (current is IResourceNode host)
                {
                    if (host.TryGetResource(key, out value))
                    {
                        return true;
                    }
                }

                current = current.ResourceParent;
            }

            value = null;
            return false;
        }

        public static IObservable<object> GetResourceObservable(this IResourceNode target, object key)
        {
            return new ResourceObservable(target, key);
        }

        private class ResourceObservable : LightweightObservableBase<object>
        {
            private readonly IResourceNode _target;
            private readonly object _key;

            public ResourceObservable(IResourceNode target, object key)
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

            protected override void Subscribed(IObserver<object> observer, bool first)
            {
                observer.OnNext(_target.FindResource(_key));
            }

            private void ResourcesChanged(object sender, ResourcesChangedEventArgs e)
            {
                PublishNext(_target.FindResource(_key));
            }
        }
    }
}
