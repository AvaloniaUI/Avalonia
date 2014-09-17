// -----------------------------------------------------------------------
// <copyright file="ITemplatedControl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling
{
    using System;

    public interface ITemplatedControl
    {
        /// <summary>
        /// Gets an observable for a <see cref="PerspexProperty"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property">The property to get the observable for.</param>
        /// <returns>The observable.</returns>
        IObservable<T> GetObservable<T>(PerspexProperty<T> property);
    }
}
