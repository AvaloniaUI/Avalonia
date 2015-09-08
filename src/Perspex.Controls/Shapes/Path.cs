





namespace Perspex.Controls.Shapes
{
    using System;
    using Perspex.Media;

    public class Path : Shape
    {
        public static readonly PerspexProperty<Geometry> DataProperty =
            PerspexProperty.Register<Path, Geometry>("Data");

        public Geometry Data
        {
            get { return this.GetValue(DataProperty); }
            set { this.SetValue(DataProperty, value); }
        }

        public override Geometry DefiningGeometry
        {
            get { return this.Data; }
        }
    }
}
