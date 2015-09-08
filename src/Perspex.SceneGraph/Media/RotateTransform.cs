// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Media
{
    /// <summary>
    /// Rotates an <see cref="IVisual"/>.
    /// </summary>
    public class RotateTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="Angle"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> AngleProperty =
            PerspexProperty.Register<RotateTransform, double>("Angle");

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        public RotateTransform()
        {
            this.GetObservable(AngleProperty).Subscribe(_ => this.RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RotateTransform"/> class.
        /// </summary>
        /// <param name="angle">The angle, in degrees.</param>
        public RotateTransform(double angle)
            : this()
        {
            this.Angle = angle;
        }

        /// <summary>
        /// Gets or sets the angle of rotation, in degrees.
        /// </summary>
        public double Angle
        {
            get { return this.GetValue(AngleProperty); }
            set { this.SetValue(AngleProperty, value); }
        }

        /// <summary>
        /// Gets the tranform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value
        {
            get { return Matrix.CreateRotation(Matrix.ToRadians(this.Angle)); }
        }
    }
}
