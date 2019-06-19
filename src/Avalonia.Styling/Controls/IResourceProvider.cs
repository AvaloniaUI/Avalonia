using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents an object that can be queried for resources.
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Raised when resources in the provider are changed.
        /// </summary>
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets a value indicating whether the element has resources.
        /// </summary>
        bool HasResources { get; }

        /// <summary>
        /// Tries to find a resource within the provider.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, null.
        /// </param>
        /// <returns>
        /// True if the resource if found, otherwise false.
        /// </returns>
        bool TryGetResource(object key, out object value);
    }
}
