// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Rectangle : Shape
    {
        static Rectangle()
        {
            AffectsGeometry<Rectangle>(BoundsProperty, StrokeThicknessProperty);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            var rect = new Rect(Bounds.Size).Deflate(StrokeThickness / 2);
            return new RectangleGeometry(rect);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(StrokeThickness, StrokeThickness);
        }
    }
}
