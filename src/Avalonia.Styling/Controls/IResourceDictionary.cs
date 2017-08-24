// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public interface IResourceDictionary : IDictionary<string, object>
    {
        /// <summary>
        /// Raised when resources in the dictionary are changed.
        /// </summary>
        event EventHandler<ResourcesChangedEventArgs> ResourcesChanged;

        /// <summary>
        /// Gets a collection of child resource dictionaries.
        /// </summary>
        IList<IResourceDictionary> MergedDictionaries { get; }

        /// <summary>
        /// Tries to find a resource within the dictionary.
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
