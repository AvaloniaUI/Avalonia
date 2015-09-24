// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Styling
{
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
