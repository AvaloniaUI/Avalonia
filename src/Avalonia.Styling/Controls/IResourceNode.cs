using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines an element that can be queried for resources.
    /// </summary>
    public interface IResourceNode
    {
        /// <summary>
        /// Raised when resources in the element are changed.
        /// </summary>
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets a value indicating whether the provider has resources.
        /// </summary>
        bool HasResources { get; }

        /// <summary>
        /// Gets the parent resource provider, if any.
        /// </summary>
        IResourceNode ResourceParent { get; }

        /// <summary>
        /// Tries to find a resource within the element.
        /// </summary>
        /// <param name="key">The resource key.</param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified key,
        /// if the key is found; otherwise, null
        /// <returns>
        /// True if the resource if found, otherwise false.
        /// </returns>
        bool TryGetResource(string key, out object value);
    }
}
