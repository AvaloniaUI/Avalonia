// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Subjects;

namespace Perspex
{
    /// <summary>
    /// Interface for getting/setting <see cref="PerspexProperty"/> bindings on an object.
    /// </summary>
    public interface IObservablePropertyBag : IPropertyBag
    {
        /// <summary>
        /// Binds a <see cref="PerspexProperty"/> to an observable.
        /// </summary>
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
        IDisposable Bind<T>(
            PerspexProperty<T> property,
            IObservable<T> source,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Initiates a two-way binding between <see cref="PerspexProperty"/>s.
        /// </summary>
        /// <param name="property">The property on this object.</param>
        /// <param name="source">The source object.</param>
        /// <param name="sourceProperty">The property on the source object.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        /// <remarks>
        /// The binding is first carried out from <paramref name="source"/> to this.
        /// </remarks>
        IDisposable BindTwoWay(
            PerspexProperty property,
            PerspexObject source,
            PerspexProperty sourceProperty,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Initiates a two-way binding between a <see cref="PerspexProperty"/> and an 
        /// <see cref="ISubject{Object}"/>.
        /// </summary>
        /// <param name="property">The property on this object.</param>
        /// <param name="source">The subject to bind to.</param>
        /// <param name="priority">The priority of the binding.</param>
        /// <returns>
        /// A disposable which can be used to terminate the binding.
        /// </returns>
        /// <remarks>
        /// The binding is first carried out from <paramref name="source"/> to this.
        /// </remarks>
        IDisposable BindTwoWay(
            PerspexProperty property,
            ISubject<object> source,
            BindingPriority priority = BindingPriority.LocalValue);

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        IObservable<object> GetObservable(PerspexProperty property);

        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="property">The property.</param>
        /// <returns>An observable.</returns>
        IObservable<T> GetObservable<T>(PerspexProperty<T> property);
    }
}