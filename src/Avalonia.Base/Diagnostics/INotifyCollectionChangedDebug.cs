// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Specialized;
using Avalonia.Collections;

namespace Avalonia.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="INotifyCollectionChanged"/> subscribers on
    /// <see cref="AvaloniaList{T}"/>
    /// </summary>
    public interface INotifyCollectionChangedDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[] GetCollectionChangedSubscribers();
    }
}
