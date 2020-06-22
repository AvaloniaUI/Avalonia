using System;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Rotates an <see cref="IVisual"/>.
    /// </summary>
    public class RotateTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="Angle"/> property.
        /// </summary>
        public static readonly StyledProperty<double> AngleProperty =
            AvaloniaProperty.Register<RotateTransform, double>(nameof(Angle));

        /// <summary>
        /// Defines the <see cref="CenterX"/> property.
        /// </summary>
        public static readonly StyledProperty<double> CenterXProperty =
            AvaloniaProperty.Register<RotateTransform, double>(nameof(CenterX));

        /// <summary>
        /// Defines the <see cref="CenterY"/> property.
        /// </summary>
        public static readonly StyledProperty<double> CenterYProperty =
            AvaloniaProperty.Register<RotateTransform, double>(nameof(CenterY));

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        public RotateTransform()
        {
            this.GetObservable(AngleProperty).Subscribe(_ => RaiseChanged());
            this.GetObservable(CenterXProperty).Subscribe(_ => RaiseChanged());
            this.GetObservable(CenterYProperty).Subscribe(_ => RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        /// <param name="angle">The angle, in degrees.</param>
        public RotateTransform(double angle)
            : this()
        {
            Angle = angle;
        }

        /// <summary>
        /// Gets or sets the angle of rotation, in degrees.
        /// </summary>
        public double Angle
        {
            get { return GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the rotation center point.
        /// </summary>
        public double CenterX
        {
            get { return GetValue(CenterXProperty); }
            set { SetValue(CenterXProperty, value); }
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the rotation center point.
        /// </summary>
        public double CenterY
        {
            get { return GetValue(CenterYProperty); }
            set { SetValue(CenterYProperty, value); }
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateRotation(Matrix.ToRadians(Angle), CenterX, CenterY);
    }
}
