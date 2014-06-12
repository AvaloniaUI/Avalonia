// -----------------------------------------------------------------------
// <copyright file="IStreamGeometryContextImpl.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;

    public interface IStreamGeometryContextImpl : IDisposable
    {
        void BeginFigure(Point startPoint, bool isFilled);

        void LineTo(Point point);

        void EndFigure(bool isClosed);
    }
}
