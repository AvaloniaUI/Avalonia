using Avalonia.Media;

namespace Avalonia.Controls.Primitives
{
    internal class TextMagnifier : Border
    {
        public Visual? Source
        {
            get;
            set
            {
                field = value;
                UpdateVisualBrush();
            }
        }

        public RelativeRect SourceRect
        {
            get;
            set
            {
                field = value;
                UpdateVisualBrush();
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            UpdateVisualBrush();
        }

        private void UpdateVisualBrush()
        {
            if (Source != null)
            {
                var visualBrush = new VisualBrush
                {
                    Visual = Source,
                    Stretch = Stretch.Uniform,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top,
                    SourceRect = SourceRect,
                    DestinationRect = new RelativeRect(new Point(), Bounds.Size, RelativeUnit.Absolute),
                };

                var drawingBrush = new DrawingBrush
                {
                    Drawing = new GeometryDrawing()
                    {
                        Geometry = new RectangleGeometry(new Rect(Bounds.Size)),
                        Brush = visualBrush
                    }
                };

                SetCurrentValue(BackgroundProperty, drawingBrush);
            }
            else
                SetCurrentValue(BackgroundProperty, null);
        }
    }
}
