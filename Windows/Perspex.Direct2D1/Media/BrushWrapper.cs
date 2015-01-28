// -----------------------------------------------------------------------
// <copyright file="BrushWrapper.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using Perspex.Media;
    using SharpDX;

    internal class BrushWrapper : ComObject
    {
        public BrushWrapper(Brush brush)
        {
            this.Brush = brush;
        }

        public Brush Brush { get; private set; }
    }
}
