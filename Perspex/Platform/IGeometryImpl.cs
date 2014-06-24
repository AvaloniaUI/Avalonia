// -----------------------------------------------------------------------
// <copyright file="IGeometryImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Platform
{
    using Perspex.Media;

    public interface IGeometryImpl
    {
        Rect Bounds { get; }

        Rect GetRenderBounds(double strokeThickness);
    }
}
