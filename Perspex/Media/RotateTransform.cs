// -----------------------------------------------------------------------
// <copyright file="RotateTransform.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    public class RotateTransform : Transform
    {
        public static readonly PerspexProperty<double> AngleProperty =
            PerspexProperty.Register<RotateTransform, double>("Angle");

        public RotateTransform()
        {
        }

        public RotateTransform(double angle)
        {
            this.Angle = angle;
        }

        public double Angle
        {
            get { return this.GetValue(AngleProperty); }
            set { this.SetValue(AngleProperty, value); }
        }

        public override Matrix Value
        {
            get { return Matrix.Rotation(Matrix.ToRadians(this.Angle)); }
        }
    }
}
