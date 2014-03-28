// -----------------------------------------------------------------------
// <copyright file="BindingExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
using Perspex.Controls;

    /// <summary>
    /// Provides binding utility extension methods.
    /// </summary>
    public static class BindingExtensions
    {
        /// <summary>
        /// Binds a property in a template to the same property in the templated parent.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="o">The control in the template.</param>
        /// <param name="templatedParent">The templated parent.</param>
        /// <param name="property">The property.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        public static IDisposable TemplateBinding<T>(
            this PerspexObject o,
            ITemplatedControl templatedParent,
            PerspexProperty<T> property)
        {
            return o.Bind(property, templatedParent.GetObservable(property), BindingPriority.TemplatedParent);
        }
    }
}
