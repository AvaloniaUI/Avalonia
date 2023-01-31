using System.Collections;
using System.Collections.Generic;

namespace Avalonia.Collections
{
    public interface IAvaloniaDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>,
        IAvaloniaReadOnlyDictionary<TKey, TValue>,
        IDictionary
        where TKey : notnull
    {
    }
}
