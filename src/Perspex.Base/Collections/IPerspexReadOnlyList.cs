// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Perspex.Collections
{
    /// <summary>
    /// A read-only notiftying list.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public interface IPerspexReadOnlyList<out T> : IReadOnlyList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
    }
}