// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Cairo;
using Perspex.Cairo.Media.Imaging;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Platform;
using Perspex.Rendering;
using Perspex.RenderHelpers;

namespace Perspex.Cairo.Media
{
    internal static class TileBrushes
    {
        public static SurfacePattern CreateTileBrush(TileBrush brush, Size targetSize)
        {
            var helper = new TileBrushImplHelper(brush, targetSize);
            if (!helper.IsValid)
                return null;
            
			using (var intermediate = new ImageSurface(Format.ARGB32, (int)helper.IntermediateSize.Width, (int)helper.IntermediateSize.Height))
            using (var ctx = new RenderTarget(intermediate).CreateDrawingContext())
            {
                helper.DrawIntermediate(ctx);

                var result = new SurfacePattern(intermediate);

                if ((brush.TileMode & TileMode.FlipXY) != 0)
                {
                    // TODO: Currently always FlipXY as that's all cairo supports natively. 
                    // Support separate FlipX and FlipY by drawing flipped images to intermediate
                    // surface.
                    result.Extend = Extend.Reflect;
                }
                else
                {
                    result.Extend = Extend.Repeat;
                }

                if (brush.TileMode != TileMode.None)
                {
                    var matrix = result.Matrix;
                    matrix.InitTranslate(-helper.DestinationRect.X, -helper.DestinationRect.Y);
                    result.Matrix = matrix;
                }

                return result;
            }
        }

      
    }
}
