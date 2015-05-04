// -----------------------------------------------------------------------
// <copyright file="MatrixTransform.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;

    public class TranslateTransform : Transform
    {
        public static readonly PerspexProperty<double> XProperty =
            PerspexProperty.Register<TranslateTransform, double>("X");

        public static readonly PerspexProperty<double> YProperty =
            PerspexProperty.Register<TranslateTransform, double>("Y");

        public TranslateTransform()
        {
            this.GetObservable(XProperty).Subscribe(_ => this.RaiseChanged());
            this.GetObservable(YProperty).Subscribe(_ => this.RaiseChanged());
        }

        public TranslateTransform(double x, double y)
            : this()
        {
            this.X = x;
            this.Y = y;
        }

        public double X
        {
            get { return this.GetValue(XProperty); }
            set { this.SetValue(XProperty, value); }
        }

        public double Y
        {
            get { return this.GetValue(YProperty); }
            set { this.SetValue(YProperty, value); }
        }

        public override Matrix Value
        {
            get { return Matrix.Translation(this.X, this.Y); }
        }
    }
}
