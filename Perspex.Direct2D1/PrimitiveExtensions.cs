// -----------------------------------------------------------------------
// <copyright file="PrimitiveExtensions.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using SharpDX;

    public static class PrimitiveExtensions
    {
        public static Rect ToPerspex(this RectangleF r)
        {
            return new Rect(r.X, r.Y, r.Width, r.Height);
        }

        public static Vector2 ToSharpDX(this Perspex.Point p)
        {
            return new Vector2((float)p.X, (float)p.Y);
        }
    }
}
