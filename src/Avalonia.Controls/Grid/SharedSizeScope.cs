// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;

namespace Avalonia.Controls
{
    /// <summary>
    /// Collection of shared states objects for a single scope
    /// </summary>
    internal class SharedSizeScope
    {
        /// <summary>
        /// Returns SharedSizeState object for a given group.
        /// Creates a new StatedState object if necessary.
        /// </summary>
        internal SharedSizeState EnsureSharedState(string sharedSizeGroup)
        {
            //  check that sharedSizeGroup is not default
            Debug.Assert(sharedSizeGroup != null);

            SharedSizeState sharedState = _registry[sharedSizeGroup] as SharedSizeState;
            if (sharedState == null)
            {
                sharedState = new SharedSizeState(this, sharedSizeGroup);
                _registry[sharedSizeGroup] = sharedState;
            }
            return (sharedState);
        }

        /// <summary>
        /// Removes an entry in the registry by the given key.
        /// </summary>
        internal void Remove(object key)
        {
            Debug.Assert(_registry.Contains(key));
            _registry.Remove(key);
        }

        private Hashtable _registry = new Hashtable();  //  storage for shared state objects
    }
}