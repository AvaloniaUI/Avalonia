// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Direct2D1.Media
{
    public class SolidColorBrushImpl : BrushImpl
    {
        public SolidColorBrushImpl(Perspex.Media.SolidColorBrush brush, SharpDX.Direct2D1.RenderTarget target)
        {
            PlatformBrush = new SharpDX.Direct2D1.SolidColorBrush(
                target,
                brush?.Color.ToDirect2D() ?? new SharpDX.Mathematics.Interop.RawColor4(),
                new SharpDX.Direct2D1.BrushProperties
                {
                    Opacity = brush != null ? (float)brush.Opacity : 1.0f,
                    Transform = target.Transform
                }
            );
        }
    }
}
