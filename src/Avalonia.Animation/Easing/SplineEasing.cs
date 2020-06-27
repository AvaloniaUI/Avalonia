namespace Avalonia.Animation.Easings
{
    /// <summary>
    /// Eases a <see cref="double"/> value
    /// using a user-defined cubic bezier curve.
    /// Good for custom easing functions that doesn't quite
    /// fit with the built-in ones. 
    /// </summary>
    public class SplineEasing : Easing
    {
        public SplineEasing()
        {
            this._internalKeySpline = new KeySpline();
        }

        public SplineEasing(double x1 = 0d, double y1 = 0d, double x2 = 1d, double y2 = 1d)
        {
            this._internalKeySpline = new KeySpline();
            this.X1 = x1;
            this.Y1 = y1;
            this.X2 = x2;
            this.Y1 = y2;
        }
        
        private KeySpline _internalKeySpline;
        private double _x1;
        private double _y1;
        private double _x2 = 1.0d;
        private double _y2 = 1.0d;
        
        /// <summary>
        /// X coordinate of the first control point
        /// </summary>
        public double X1
        {
            get => _x1;
            set
            {
                _x1 = value; _internalKeySpline.ControlPointX1 = _x1;
            }
        }

        /// <summary>
        /// Y coordinate of the first control point
        /// </summary>
        public double Y1
        {
            get => _y1;
            set
            {
                _y1 = value; _internalKeySpline.ControlPointY1 = _y1;
            }
        }

        /// <summary>
        /// X coordinate of the second control point
        /// </summary>
        public double X2
        {
            get => _x2;
            set
            {
                _x2 = value;
                _internalKeySpline.ControlPointX2 = _x2;
            }
        }

        /// <summary>
        /// Y coordinate of the second control point
        /// </summary>
        public double Y2
        {
            get => _y2;
            set
            {
                _y2 = value;
                _internalKeySpline.ControlPointY2 = _y2;
            }
        }
        
        /// <inheritdoc/>
        public override double Ease(double progress) =>
            _internalKeySpline.GetSplineProgress(progress);
    }
}
