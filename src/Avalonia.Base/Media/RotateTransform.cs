using System;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Media
{
    /// <summary>
    /// Rotates an <see cref="Visual"/>.
    /// </summary>
    public sealed class RotateTransform : Transform
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
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        /// <param name="angle">The angle, in degrees.</param>
        /// <param name="centerX">The x-coordinate of the center point for the rotation.</param>
        /// <param name="centerY">The y-coordinate of the center point for the rotation.</param>
        public RotateTransform(double angle, double centerX, double centerY)
            : this()
        {
            Angle = angle;
            CenterX = centerX;
            CenterY = centerY;
        }

        /// <summary>
        /// Gets or sets the angle of rotation, in degrees.
        /// </summary>
        public double Angle
        {
            get => GetValue(AngleProperty);
            set => SetValue(AngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the rotation center point. The default is 0.
        /// </summary>
        public double CenterX
        {
            get => GetValue(CenterXProperty);
            set => SetValue(CenterXProperty, value);
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the rotation center point. The default is 0.
        /// </summary>
        public double CenterY
        {
            get => GetValue(CenterYProperty);
            set => SetValue(CenterYProperty, value);
        }

        /// <summary>
        /// Gets the transform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value => Matrix.CreateTranslation(-CenterX, -CenterY) *
            Matrix.CreateRotation(Matrix.ToRadians(Angle)) *
            Matrix.CreateTranslation(CenterX, CenterY);
    }
}
