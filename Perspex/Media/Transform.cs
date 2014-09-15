// -----------------------------------------------------------------------
// <copyright file="Transform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public abstract class Transform : PerspexObject
    {
        public abstract Matrix Value { get; }
    }
}
