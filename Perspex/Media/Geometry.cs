// -----------------------------------------------------------------------
// <copyright file="Geometry.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public abstract class Geometry
    {
        public abstract Rect Bounds
        {
            get;
        }

        public IGeometryImpl PlatformImpl
        {
            get;
            protected set;
        }
    }
}
