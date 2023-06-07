using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Collections
{
    /// <summary>
    /// A read-only notifying list.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public interface IAvaloniaReadOnlyList<out T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}
