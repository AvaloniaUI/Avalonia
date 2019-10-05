using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace RenderDemo.Pages
{
    public class DataRepeaterBorderLine : Shape
    {
        public static readonly DirectProperty<DataRepeaterBorderLine, Orientation> OrientationProperty =
            AvaloniaProperty.RegisterDirect<DataRepeaterBorderLine, Orientation>(
                nameof(Orientation),
                o => o.Orientation,
                (o, v) => o.Orientation = v);

        private Orientation _orientation;

        public Orientation Orientation
        {
            get => _orientation;
            set => SetAndRaise(OrientationProperty, ref _orientation, value);
        }

        public static readonly DirectProperty<DataRepeaterBorderLine, double> LengthProperty =
            AvaloniaProperty.RegisterDirect<DataRepeaterBorderLine, double>(
                nameof(Length),
                o => o.Length,
                (o, v) => o.Length = v);

        private double _length;

        public double Length
        {
            get => _length;
            set => SetAndRaise(LengthProperty, ref _length, value);
        }

        static DataRepeaterBorderLine()
        {
            AffectsGeometry<DataRepeaterBorderLine>(LengthProperty, OrientationProperty);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return new Size(1, 1);
        }

        protected override Geometry CreateDefiningGeometry()
        {
            if (_orientation == Orientation.Horizontal)
                return new RectangleGeometry(new Rect(0, 0, _length, 1));
            else
                return new RectangleGeometry(new Rect(0, 0, 1, _length));
        }
    }
}
