// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Handles binding to <see cref="IObservable{T}"/>s for the '^' stream binding operator.
    /// </summary>
    public class ObservableStreamPlugin : IStreamPlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        public virtual bool Match(WeakReference reference) => reference.Target is IObservable<object>;

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        public virtual IObservable<object> Start(WeakReference reference)
        {
            return reference.Target as IObservable<object>;
        }
    }
}
