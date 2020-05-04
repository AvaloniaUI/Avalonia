using System;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Path : Shape
    {
        public static readonly StyledProperty<Geometry> DataProperty =
            AvaloniaProperty.Register<Path, Geometry>(nameof(Data));

        static Path()
        {
            AffectsGeometry<Path>(DataProperty);
            DataProperty.Changed.AddClassHandler<Path>((o, e) => o.DataChanged(e));
        }

        public Geometry Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        protected override Geometry CreateDefiningGeometry() => Data;

        private void DataChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var oldGeometry = (Geometry)e.OldValue;
            var newGeometry = (Geometry)e.NewValue;

            if (oldGeometry is object)
            {
                oldGeometry.Changed -= GeometryChanged;
            }

            if (newGeometry is object)
            {
                newGeometry.Changed += GeometryChanged;
            }
        }

        private void GeometryChanged(object sender, EventArgs e)
        {
            InvalidateGeometry();
        }
    }
}
