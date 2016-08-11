// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Data
{
    /// <summary>
    /// Holds a binding that can be applied to a property on an object.
    /// </summary>
    public interface IBinding
    {
        /// <summary>
        /// Initiates the binding on a target object.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetProperty">The target property. May be null.</param>
        /// <param name="anchor">
        /// An optional anchor from which to locate required context. When binding to objects that
        /// are not in the logical tree, certain types of binding need an anchor into the tree in 
        /// order to locate named controls or resources. The <paramref name="anchor"/> parameter 
        /// can be used to provice this context.
        /// </param>
        /// <param name="enableDataValidation">Whether data validation should be enabled.</param>
        /// <returns>
        /// A <see cref="InstancedBinding"/> or null if the binding could not be resolved.
        /// </returns>
        InstancedBinding Initiate(
            IAvaloniaObject target, 
            AvaloniaProperty targetProperty,
            object anchor = null,
            bool enableDataValidation = false);
    }
}
