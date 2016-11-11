// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Defines a plugin that handles the '^' stream binding operator.
    /// </summary>
    public interface IStreamPlugin
    {
        /// <summary>
        /// Checks whether this plugin handles the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the value.</param>
        /// <returns>True if the plugin can handle the value; otherwise false.</returns>
        bool Match(WeakReference reference);

        /// <summary>
        /// Starts producing output based on the specified value.
        /// </summary>
        /// <param name="reference">A weak reference to the object.</param>
        /// <returns>
        /// An observable that produces the output for the value.
        /// </returns>
        IObservable<object> Start(WeakReference reference);
    }
}
