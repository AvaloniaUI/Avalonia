// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Perspex.Markup.Xaml.Data
{
    /// <summary>
    /// Defines a binding that can be created in XAML markup.
    /// </summary>
    public interface IXamlBinding
    {
        /// <summary>
        /// Applies the binding to a property on an instance.
        /// </summary>
        /// <param name="instance">The target instance.</param>
        /// <param name="property">The target property.</param>
        void Bind(IObservablePropertyBag instance, PerspexProperty property);

        /// <summary>
        /// Creates a subject that can be used to get and set the value of the binding.
        /// </summary>
        /// <param name="target">The target instance.</param>
        /// <param name="targetType">The type of the target property.</param>
        /// <param name="targetIsDataContext">
        /// Whether the target property is the DataContext property.
        /// </param>
        /// <returns>An <see cref="ISubject{object}"/>.</returns>
        ISubject<object> CreateSubject(
            IObservablePropertyBag target,
            Type targetType,
            bool targetIsDataContext = false);
    }
}