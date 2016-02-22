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
        /// <param name="treeAnchor">
        /// For `ElementName` bindings to elements that are not themselves controls, describes
        /// where in the logical tree to begin searching for the named element.
        /// </param>
        /// <returns>An <see cref="ISubject{Object}"/>.</returns>
        ISubject<object> CreateSubject(
            IPerspexObject target, 
            PerspexProperty targetProperty,
            IPerspexObject treeAnchor = null);
    }
}
