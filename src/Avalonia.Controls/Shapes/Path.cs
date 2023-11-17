using System;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Path : Shape
    {
        public static readonly StyledProperty<Geometry> DataProperty =
            AvaloniaProperty.Register<Path, Geometry>(nameof(Data));

        private EventHandler? _geometryChangedHandler;

        static Path()
        {
            AffectsGeometry<Path>(DataProperty);
            DataProperty.Changed.AddClassHandler<Path>((o, e) => o.DataChanged(e));
        }

        public Geometry Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        private EventHandler? GeometryChangedHandler => _geometryChangedHandler ??= GeometryChanged;

        protected override Geometry CreateDefiningGeometry() => Data;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (Data is object)
            {
                Data.Changed += GeometryChangedHandler;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (Data is object)
            {
                Data.Changed -= GeometryChangedHandler;
            }
        }

        private void DataChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (VisualRoot is null)
            {
                return;
            }

            var oldGeometry = (Geometry?)e.OldValue;
            var newGeometry = (Geometry?)e.NewValue;

            if (oldGeometry is object)
            {
                oldGeometry.Changed -= GeometryChangedHandler;
            }

            if (newGeometry is object)
            {
                newGeometry.Changed += GeometryChangedHandler;
            }
        }

        private void GeometryChanged(object? sender, EventArgs e)
        {
            InvalidateGeometry();
        }
    }
}
