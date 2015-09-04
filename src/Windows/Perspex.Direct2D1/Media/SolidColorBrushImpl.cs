// -----------------------------------------------------------------------
// <copyright file="SolidColorBrushImpl.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Direct2D1.Media
{
    public class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, SharpDX.Direct2D1.RenderTarget target)
        {
            this.PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(target, brush?.Color.ToDirect2D() ?? new SharpDX.Color4());
        }
    }
}
