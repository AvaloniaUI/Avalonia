using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Collections
{
    public interface IAvaloniaReadOnlyDictionary<TKey, TValue>
        : IReadOnlyDictionary<TKey, TValue>,
        INotifyCollectionChanged,
        INotifyPropertyChanged
        where TKey : notnull
    {
    }
}
