// -----------------------------------------------------------------------
// <copyright file="ReadOnlyPerspexProperty.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    public class ReadOnlyPerspexProperty<T>
    {
        public ReadOnlyPerspexProperty(PerspexProperty property)
        {
            this.Property = property;
        }

        internal PerspexProperty Property { get; private set; }
    }
}
