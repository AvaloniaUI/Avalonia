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
        public bool TryGetResource(string key, out object value) => TryGetValue(key, out value);
    }
}
