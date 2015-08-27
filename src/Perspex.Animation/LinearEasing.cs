// -----------------------------------------------------------------------
// <copyright file="LinearEasing.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

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
                throw new NotSupportedException(string.Format(
                    "Don't know how to create a LinearEasing for type '{0}'.",
                    typeof(T).FullName));
            }
        }
    }
}
