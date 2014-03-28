// -----------------------------------------------------------------------
// <copyright file="IStyleable.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;
    using Perspex.Controls;

    /// <summary>
    /// Interface for styleable elements.
    /// </summary>
    public interface IStyleable
    {
        /// <summary>
        /// Gets the list of classes for the control.
        /// </summary>
        Classes Classes { get; }

        /// <summary>
        /// Gets the ID of the control.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the template parent of this element if the control comes from a template.
        /// </summary>
        ITemplatedControl TemplatedParent { get; }

        /// <summary>
        /// Binds a <see cref="PerspexProperty"/> to an observable.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="source">The observable.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        IDisposable Bind(
            PerspexProperty property, 
            IObservable<object> source, 
            BindingPriority priority = BindingPriority.LocalValue);
    }
}
