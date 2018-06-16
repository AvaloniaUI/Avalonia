// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Represents the geometry of a rectangle.
    /// </summary>
    public class RectangleGeometry : Geometry
    {
        /// <summary>
        /// Defines the <see cref="Rect"/> property.
        /// </summary>
        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<RectangleGeometry, Rect>(nameof(Rect));

        public Rect Rect
        {
            get => GetValue(RectProperty);
            set => SetValue(RectProperty, value);
        }

        static RectangleGeometry()
        {
            AffectsGeometry(RectProperty);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        public RectangleGeometry()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect)
        {
            Rect = rect;
        }

        /// <inheritdoc/>
        public override Geometry Clone() => new RectangleGeometry(Rect);

        protected override IGeometryImpl CreateDefiningGeometry()
        {
            var factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            var geometry = factory.CreateStreamGeometry();

            using (var context = geometry.Open())
            {
                var rect = Rect;
                context.BeginFigure(rect.TopLeft, true);
                context.LineTo(rect.TopRight);
                context.LineTo(rect.BottomRight);
                context.LineTo(rect.BottomLeft);
                context.EndFigure(true);
            }

            return geometry;
        }
    }
}
