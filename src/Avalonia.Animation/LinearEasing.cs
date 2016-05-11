// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Animation
{
    /// <summary>
    /// Returns a linear <see cref="IEasing"/> for the specified type.
    /// </summary>
    /// <remarks>
    /// Unfortunately this class is needed as there's no way to create a true generic easing
    /// function at compile time, as mathematical operators don't have an interface.
    /// </remarks>
    public static class LinearEasing
    {
        /// <summary>
        /// A linear easing function for the specified type.
        /// </summary>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>An easing function.</returns>
        public static IEasing<T> For<T>()
        {
            if (typeof(T) == typeof(double))
            {
                return (IEasing<T>)new LinearDoubleEasing();
            }
            else
            {
                throw new NotSupportedException(
                    $"Don't know how to create a LinearEasing for type '{typeof(T).FullName}'.");
            }
        }
    }
}
