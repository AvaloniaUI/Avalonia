// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Data;

namespace Perspex
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

        /// <summary>
        /// Called when the validation state of a <see cref="PriorityValue"/> changes.
        /// </summary>
        /// <param name="sender">The source of the change.</param>
        /// <param name="status">The validation status.</param>
        void DataValidationChanged(PriorityValue sender, IValidationStatus status);
    }
}
