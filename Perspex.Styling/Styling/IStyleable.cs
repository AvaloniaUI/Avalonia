// -----------------------------------------------------------------------
// <copyright file="IStyleable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;

    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    public interface IStyleable : IObservablePropertyBag
    {
        /// <summary>
        /// Gets the list of classes for the control.
        /// </summary>
        Classes Classes { get; }

        /// <summary>
        /// Gets the ID of the control.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the type by which the control is styled.
        /// </summary>
        Type StyleKey { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        ITemplatedControl TemplatedParent { get; }
    }
}
