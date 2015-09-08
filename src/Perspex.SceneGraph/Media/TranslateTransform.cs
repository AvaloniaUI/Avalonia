





namespace Perspex.Media
{
    using System;

    /// <summary>
    /// Translates (moves) an <see cref="IVisual"/>.
    /// </summary>
    public class TranslateTransform : Transform
    {
        /// <summary>
        /// Defines the <see cref="X"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> XProperty =
            PerspexProperty.Register<TranslateTransform, double>("X");

        /// <summary>
        /// Defines the <see cref="Y"/> property.
        /// </summary>
        public static readonly PerspexProperty<double> YProperty =
            PerspexProperty.Register<TranslateTransform, double>("Y");

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateTransform"/> class.
        /// </summary>
        public TranslateTransform()
        {
            this.GetObservable(XProperty).Subscribe(_ => this.RaiseChanged());
            this.GetObservable(YProperty).Subscribe(_ => this.RaiseChanged());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslateTransform"/> class.
        /// </summary>
        /// <param name="x">Gets the horizontal offset of the translate.</param>
        /// <param name="y">Gets the vertical offset of the translate.</param>
        public TranslateTransform(double x, double y)
            : this()
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// Gets the horizontal offset of the translate.
        /// </summary>
        public double X
        {
            get { return this.GetValue(XProperty); }
            set { this.SetValue(XProperty, value); }
        }

        /// <summary>
        /// Gets the vertical offset of the translate.
        /// </summary>
        public double Y
        {
            get { return this.GetValue(YProperty); }
            set { this.SetValue(YProperty, value); }
        }

        /// <summary>
        /// Gets the tranform's <see cref="Matrix"/>.
        /// </summary>
        public override Matrix Value
        {
            get { return Matrix.CreateTranslation(this.X, this.Y); }
        }
    }
}
