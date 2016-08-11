// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;

namespace Avalonia
{
    /// <summary>
    /// An owner of a <see cref="PriorityValue"/>.
    /// </summary>
    internal interface IPriorityValueOwner
    {
        /// <summary>
        /// Called when a <see cref="PriorityValue"/>'s value changes.
        /// </summary>
        /// <param name="sender">The source of the change.</param>
        /// <param name="oldValue">The old value.</param>
        /// <param name="newValue">The new value.</param>
        void Changed(PriorityValue sender, object oldValue, object newValue);
    }
}
