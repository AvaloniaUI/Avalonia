// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Subjects;

namespace Perspex.Data
{
    /// <summary>
    /// Holds a binding that can be applied to a property on an object.
    /// </summary>
    public interface IBinding
    {
        /// <summary>
        /// Gets the binding mode.
        /// </summary>
        BindingMode Mode { get; }

        /// <summary>
        /// Gets the binding priority.
        /// </summary>
        BindingPriority Priority { get; }

        /// <summary>
        /// Creates a subject that can be used to get and set the value of the binding.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetProperty">The target property. May be null.</param>
        /// <param name="anchor">An optional anchor from which to locate required context.</param>
        /// <returns>An <see cref="ISubject{Object}"/>.</returns>
        /// <remarks>
        /// When binding to objects that are not in the logical tree, certain types of binding need
        /// an anchor into the tree in order to locate named controls or resources. The
        /// <paramref name="anchor"/> parameter can be used to provice this context.
        /// </remarks>
        ISubject<object> CreateSubject(
            IPerspexObject target, 
            PerspexProperty targetProperty,
            object anchor = null);
    }
}
