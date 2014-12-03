// -----------------------------------------------------------------------
// <copyright file="CairoPlatform.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo
{
    using Cairo = global::Cairo;

    public static class CairoExtensions
    {
        public static Cairo.Matrix ToCairo(this Matrix m)
        {
            return new Cairo.Matrix(m.M11, m.M12, m.M21, m.M22, m.OffsetX, m.OffsetY);
        }

        public static Cairo.Rectangle ToCairo(this Rect rect)
        {
            return new Cairo.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}

