// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Avalonia.Media
{
    public class CompositeTransform : Transform
    {
        private Transform[] _innerTransforms = new Transform[4];

        /// <summary>
        /// Gets the transform's <see cref="Matrix" />.
        /// </summary>
        public override Matrix Value
        {
            get
            {
                var result = Matrix.Identity;

                for (int i = 0; i < 4; i++)
                {
                    var t = _innerTransforms[i] as Transform;
                    if (t != null)
                        result *= t.Value;
                }

                return result;
            }
        }

        public static readonly StyledProperty<double> ScaleXProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(ScaleX), defaultValue: 1);

        public double ScaleX
        {
            get { return GetValue(ScaleXProperty); }
            set { SetValue(ScaleXProperty, value); }
        }

        public static readonly StyledProperty<double> ScaleYProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(ScaleY), defaultValue: 1);

        public double ScaleY
        {
            get { return GetValue(ScaleYProperty); }
            set { SetValue(ScaleYProperty, value); }
        }

        public static readonly StyledProperty<double> SkewXProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(SkewX));

        public double SkewX
        {
            get { return GetValue(SkewXProperty); }
            set { SetValue(SkewXProperty, value); }
        }

        public static readonly StyledProperty<double> SkewYProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(SkewY));

        public double SkewY
        {
            get { return GetValue(SkewYProperty); }
            set { SetValue(SkewYProperty, value); }
        }

        public static readonly StyledProperty<double> TranslateXProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(TranslateX));

        public double TranslateX
        {
            get { return GetValue(TranslateXProperty); }
            set { SetValue(TranslateXProperty, value); }
        }

        public static readonly StyledProperty<double> TranslateYProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(TranslateY));

        public double TranslateY
        {
            get { return GetValue(TranslateYProperty); }
            set { SetValue(TranslateYProperty, value); }
        }

        public static readonly StyledProperty<double> RotationProperty =
               AvaloniaProperty.Register<CompositeTransform, double>(nameof(Rotation));

        public double Rotation
        {
            get { return GetValue(RotationProperty); }
            set { SetValue(RotationProperty, value); }
        }

        static CompositeTransform()
        {
            ScaleXProperty.Changed.AddClassHandler<CompositeTransform>(ScaleXChanged);
            ScaleYProperty.Changed.AddClassHandler<CompositeTransform>(ScaleYChanged);

            SkewXProperty.Changed.AddClassHandler<CompositeTransform>(SkewXChanged);
            SkewYProperty.Changed.AddClassHandler<CompositeTransform>(SkewYChanged);

            RotationProperty.Changed.AddClassHandler<CompositeTransform>(RotationChanged);

            TranslateXProperty.Changed.AddClassHandler<CompositeTransform>(TranslateXChanged);
            TranslateYProperty.Changed.AddClassHandler<CompositeTransform>(TranslateYChanged);
        }

        private static void ScaleXChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeScale();
            var sT = transform._innerTransforms[0] as ScaleTransform;
            sT.ScaleX = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void ScaleYChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeScale();
            var sT = transform._innerTransforms[0] as ScaleTransform;
            sT.ScaleY = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void SkewXChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeSkew();
            var sT = transform._innerTransforms[1] as SkewTransform;
            sT.AngleX = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void SkewYChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeSkew();
            var sT = transform._innerTransforms[1] as SkewTransform;
            sT.AngleY = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void RotationChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeRotation();
            var rT = transform._innerTransforms[2] as RotateTransform;
            rT.Angle = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void TranslateXChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeTranslate();
            var tT = transform._innerTransforms[3] as TranslateTransform;
            tT.X = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private static void TranslateYChanged(CompositeTransform transform, AvaloniaPropertyChangedEventArgs e)
        {
            transform.InitializeTranslate();
            var tT = transform._innerTransforms[3] as TranslateTransform;
            tT.Y = (double)e.NewValue;
            transform.RaiseChanged();
        }

        private void InitializeScale()
        {
            if (_innerTransforms[0] == null)
            {
                _innerTransforms[0] = new ScaleTransform();
            }
        }

        private void InitializeSkew()
        {
            if (_innerTransforms[1] == null)
            {
                _innerTransforms[1] = new SkewTransform();
            }
        }

        private void InitializeRotation()
        {
            if (_innerTransforms[2] == null)
            {
                _innerTransforms[2] = new RotateTransform();
            }
        }

        private void InitializeTranslate()
        {
            if (_innerTransforms[3] == null)
            {
                _innerTransforms[3] = new TranslateTransform();
            }
        }
    }
}
