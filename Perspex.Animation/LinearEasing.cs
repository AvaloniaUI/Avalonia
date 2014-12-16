// -----------------------------------------------------------------------
// <copyright file="PropertyTransition.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Animation
{
    using System;

    public static class LinearEasing
    {
        public static LinearDoubleEasing For<T>()
        {
            if (typeof(T) == typeof(double))
            {
                return new LinearDoubleEasing();
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
