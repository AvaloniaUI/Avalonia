// -----------------------------------------------------------------------
// <copyright file="BrushImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    using System;

    public abstract class BrushImpl : IDisposable
    {
        public SharpDX.Direct2D1.Brush PlatformBrush { get; set; }

        public virtual void Dispose()
        {
            if (this.PlatformBrush != null)
            {
                this.PlatformBrush.Dispose();
            }
        }
    }
}
