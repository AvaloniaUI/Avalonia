using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

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
            Contract.Requires<ArgumentNullException>(control != null);
            Contract.Requires<ArgumentNullException>(key != null);

            var current = control;

            while (current != null)
            {
                if (current is IResourceNode host)
                {
                    if (host.TryGetResource(key, out var value))
                    {
                        return value;
                    }
                }

                current = current.ResourceParent;
            }

            return AvaloniaProperty.UnsetValue;
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
