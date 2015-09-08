// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Direct2D1.Media
{
    public class LinearGradientBrushImpl : BrushImpl
    {
        public LinearGradientBrushImpl(
            Perspex.Media.LinearGradientBrush brush,
            SharpDX.Direct2D1.RenderTarget target,
            Size destinationSize)
        {
            if (brush != null)
            {
                var gradientStops = brush.GradientStops.Select(s => new SharpDX.Direct2D1.GradientStop { Color = s.Color.ToDirect2D(), Position = (float)s.Offset }).ToArray();

                Point startPoint = new Point(0, 0);
                Point endPoint = new Point(0, 0);

                switch (brush.MappingMode)
                {
                    case Perspex.Media.BrushMappingMode.Absolute:
                        // TODO:

                        break;
                    case Perspex.Media.BrushMappingMode.RelativeToBoundingBox:
                        startPoint = new Point(brush.StartPoint.X * destinationSize.Width, brush.StartPoint.Y * destinationSize.Height);
                        endPoint = new Point(brush.EndPoint.X * destinationSize.Width, brush.EndPoint.Y * destinationSize.Height);

                        break;
                }

                this.PlatformBrush = new SharpDX.Direct2D1.LinearGradientBrush(
                    target,
                    new SharpDX.Direct2D1.LinearGradientBrushProperties { StartPoint = startPoint.ToSharpDX(), EndPoint = endPoint.ToSharpDX() },
                    new SharpDX.Direct2D1.BrushProperties { Opacity = (float)brush.Opacity, Transform = target.Transform },
                    new SharpDX.Direct2D1.GradientStopCollection(target, gradientStops, brush.SpreadMethod.ToDirect2D())
                );
            }
        }
    }
}
