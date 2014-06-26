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
        Rect Bounds { get; set; }

        IEnumerable<IVisual> ExistingVisualChildren { get; }

        bool IsVisible { get; }

        IEnumerable<IVisual> VisualChildren { get; }

        IVisual VisualParent { get; set; }

        void Render(IDrawingContext context);
    }
}
