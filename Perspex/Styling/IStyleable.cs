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
        /// Binds a <see cref="PerspexProperty"/> to a style.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="value">The activated value.</param>
        /// <param name="activator">An observable which activates the value.</param>
        /// <remarks>
        /// Style bindings have a lower precedence than local value bindings. They are toggled
        /// on or off by <paramref name="activator"/> and can be unbound by the activator 
        /// completing.
        /// </remarks>
        void SetValue(PerspexProperty property, object value, IObservable<bool> activator);
    }
}
