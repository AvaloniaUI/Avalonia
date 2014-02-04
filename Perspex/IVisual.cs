// -----------------------------------------------------------------------
// <copyright file="IVisual.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex
{
    using System;
    using System.Collections.Generic;
    using Perspex.Media;

    public interface IVisual
    {
        Rect Bounds { get; }
        
        IEnumerable<IVisual> VisualChildren { get; }
        
        IVisual VisualParent { get; }

        void Render(IDrawingContext context);
    }
}
