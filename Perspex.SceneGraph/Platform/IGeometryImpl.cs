// -----------------------------------------------------------------------
// <copyright file="IGeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    public interface IGeometryImpl
    {
        Rect Bounds { get; }

        Rect GetRenderBounds(double strokeThickness);
    }
}
