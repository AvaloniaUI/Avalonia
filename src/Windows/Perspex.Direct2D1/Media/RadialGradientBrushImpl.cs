// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Direct2D1.Media
{
    public class RadialGradientBrushImpl : BrushImpl
    {
        public RadialGradientBrushImpl(
            Perspex.Media.RadialGradientBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size destinationSize)
        {
            var gradientStops = brush.GradientStops.Select(s => new SharpDX.Direct2D1.GradientStop { Color = s.Color.ToDirect2D(), Position = (float)s.Offset }).ToArray();

            Point centerPoint = brush.Center.ToPixels(destinationSize);
            Point GradientOriginOffset = brush.GradientOrigin.ToPixels(destinationSize);
            // Note: Direct2D supports RadiusX and RadiusY but Cairo backend supports only Radius property
            double radiusX = brush.Radius;
            double radiusY = brush.Radius;

            PlatformBrush = new SharpDX.Direct2D1.RadialGradientBrush(
                target,
                new SharpDX.Direct2D1.RadialGradientBrushProperties { Center = centerPoint.ToSharpDX(), GradientOriginOffset = GradientOriginOffset.ToSharpDX(), RadiusX = (float)radiusX, RadiusY = (float)radiusY },
                new SharpDX.Direct2D1.BrushProperties { Opacity = (float)brush.Opacity, Transform = target.Transform },
                new SharpDX.Direct2D1.GradientStopCollection(target, gradientStops, brush.SpreadMethod.ToDirect2D())
            );
        }
    }
}
