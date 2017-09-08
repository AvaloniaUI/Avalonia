using System;
using System.Reactive;
using System.Reactive.Linq;

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
        public static object FindResource(this IResourceNode control, string key)
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
        public static bool TryFindResource(this IResourceNode control, string key, out object value)
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

        public static IObservable<object> GetResourceObservable(this IResourceNode target, string key)
        {
            return Observable.FromEventPattern<ResourcesChangedEventArgs>(
                x => target.ResourcesChanged += x,
                x => target.ResourcesChanged -= x)
                .StartWith((EventPattern<ResourcesChangedEventArgs>)null)
                .Select(x => target.FindResource(key));
        }
    }
}
