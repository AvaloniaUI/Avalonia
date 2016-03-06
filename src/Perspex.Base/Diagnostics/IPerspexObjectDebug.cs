// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Diagnostics
{
    /// <summary>
    /// Provides a debug interface into <see cref="PerspexObject"/>.
    /// </summary>
    public interface IPerspexObjectDebug
    {
        /// <summary>
        /// Gets the subscriber list for the <see cref="IPerspexObject.PropertyChanged"/>
        /// event.
        /// </summary>
        /// <returns>
        /// The subscribers or null if no subscribers.
        /// </returns>
        Delegate[] GetPropertyChangedSubscribers();
    }
}
