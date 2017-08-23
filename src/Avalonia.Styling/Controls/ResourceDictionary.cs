// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections;
using Avalonia.Collections;

namespace Avalonia.Controls
{
    /// <summary>
    /// An indexed dictionary of resources.
    /// </summary>
    public class ResourceDictionary : AvaloniaDictionary<string, object>, IResourceDictionary, IDictionary
    {
        /// <inheritdoc/>
        public bool TryGetResource(string key, out object value) => TryGetValue(key, out value);
    }
}
