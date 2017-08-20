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
            RectProperty.Changed.AddClassHandler<RectangleGeometry>(x => x.RectChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        public RectangleGeometry()
        {
            IPlatformRenderInterface factory = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();
            PlatformImpl = factory.CreateStreamGeometry();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleGeometry"/> class.
        /// </summary>
        /// <param name="rect">The rectangle bounds.</param>
        public RectangleGeometry(Rect rect) : this()
        {
            Rect = rect;
        }

        /// <inheritdoc/>
        public override Geometry Clone()
        {
            return new RectangleGeometry(Rect);
        }

        private void RectChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var rect = (Rect)e.NewValue;
            using (var context = ((IStreamGeometryImpl)PlatformImpl).Open())
            {
                context.BeginFigure(rect.TopLeft, true);
                context.LineTo(rect.TopRight);
                context.LineTo(rect.BottomRight);
                context.LineTo(rect.BottomLeft);
                context.EndFigure(true);
            }
        }
    }
}
