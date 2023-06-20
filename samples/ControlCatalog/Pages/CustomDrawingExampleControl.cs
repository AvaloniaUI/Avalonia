using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Controls.Shapes;

namespace ControlCatalog.Pages
{
    public class CustomDrawingExampleControl : Control
    {
        private Point _cursorPoint;


        public static readonly StyledProperty<double> ScaleProperty = AvaloniaProperty.Register<CustomDrawingExampleControl, double>(nameof(Scale), 1.0d);
        public double Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

        public static readonly StyledProperty<double> RotationProperty = AvaloniaProperty.Register<CustomDrawingExampleControl, double>(nameof(Rotation),
            coerce: (_, val) => val % (Math.PI * 2));

        /// <summary>
        /// Rotation, measured in Radians!
        /// </summary>
        public double Rotation
        {
            get => GetValue(RotationProperty);
            set => SetValue(RotationProperty, value);
        }

        public static readonly StyledProperty<double> ViewportCenterYProperty = AvaloniaProperty.Register<CustomDrawingExampleControl, double>(nameof(ViewportCenterY), 0.0d);
        public double ViewportCenterY { get => GetValue(ViewportCenterYProperty); set => SetValue(ViewportCenterYProperty, value); }

        public static readonly StyledProperty<double> ViewportCenterXProperty = AvaloniaProperty.Register<CustomDrawingExampleControl, double>(nameof(ViewportCenterX), 0.0d);
        public double ViewportCenterX { get => GetValue(ViewportCenterXProperty); set => SetValue(ViewportCenterXProperty, value); }

        private IPen _pen;

        private System.Diagnostics.Stopwatch _timeKeeper = System.Diagnostics.Stopwatch.StartNew();

        private bool _isPointerCaptured = false;

        public CustomDrawingExampleControl()
        {
            _pen = new Pen(new SolidColorBrush(Colors.Black), lineCap: PenLineCap.Round);

            var _arc = new ArcSegment()
            {
                IsLargeArc = false,
                Point = new Point(0, 0),
                RotationAngle = 0,
                Size = new Size(25, 25),
                SweepDirection = SweepDirection.Clockwise,

            };
            StreamGeometry sg = new StreamGeometry();
            using (var cntx = sg.Open())
            {
                cntx.BeginFigure(new Point(-25.0d, -10.0d), false);
                cntx.ArcTo(new Point(25.0d, -10.0d), new Size(10.0d, 10.0d), 0.0d, false, SweepDirection.Clockwise);
                cntx.EndFigure(true);
            }
            _smileGeometry = sg.Clone();
        }

        private Geometry _smileGeometry;

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            Point previousPoint = _cursorPoint;

            _cursorPoint = e.GetPosition(this);

            if (_isPointerCaptured)
            {
                Point oldWorldPoint = UIPointToWorldPoint(previousPoint, ViewportCenterX, ViewportCenterY, Scale, Rotation);
                Point newWorldPoint = UIPointToWorldPoint(_cursorPoint, ViewportCenterX, ViewportCenterY, Scale, Rotation);

                Vector diff = newWorldPoint - oldWorldPoint;

                ViewportCenterX -= diff.X;
                ViewportCenterY -= diff.Y;
            }
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            e.Handled = true;
            e.Pointer.Capture(this);
            _isPointerCaptured = true;
            base.OnPointerPressed(e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            var oldScale = Scale;
            Scale *= (1.0d + e.Delta.Y / 12.0d);

            Point oldWorldPoint = UIPointToWorldPoint(_cursorPoint, ViewportCenterX, ViewportCenterY, oldScale, Rotation);
            Point newWorldPoint = UIPointToWorldPoint(_cursorPoint, ViewportCenterX, ViewportCenterY, Scale, Rotation);

            Vector diff = newWorldPoint - oldWorldPoint;

            ViewportCenterX -= diff.X;
            ViewportCenterY -= diff.Y;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            e.Pointer.Capture(null);
            _isPointerCaptured = false;
            base.OnPointerReleased(e);
        }

        public override void Render(DrawingContext context)
        {
            var localBounds = new Rect(new Size(this.Bounds.Width, this.Bounds.Height));
            var clip = context.PushClip(this.Bounds);
            context.DrawRectangle(Brushes.White, _pen, localBounds, 1.0d);

            var halfMax = Math.Max(this.Bounds.Width / 2.0d, this.Bounds.Height / 2.0d) * Math.Sqrt(2.0d);
            var halfMin = Math.Min(this.Bounds.Width / 2.0d, this.Bounds.Height / 2.0d) / 1.3d;
            var halfWidth = this.Bounds.Width / 2.0d;
            var halfHeight = this.Bounds.Height / 2.0d;

            // 0,0 refers to the top-left of the control now. It is not prime time to draw gui stuff because it'll be under the world 

            var translateModifier = context.PushTransform(Avalonia.Matrix.CreateTranslation(new Avalonia.Vector(halfWidth, halfHeight)));

            // now 0,0 refers to the ViewportCenter(X,Y). 
            var rotationMatrix = Avalonia.Matrix.CreateRotation(Rotation);
            var rotationModifier = context.PushTransform(rotationMatrix);

            // everything is rotated but not scaled 

            var scaleModifier = context.PushTransform(Avalonia.Matrix.CreateScale(Scale, -Scale));

            var mapPositionModifier = context.PushTransform(Matrix.CreateTranslation(new Vector(-ViewportCenterX, -ViewportCenterY)));

            // now everything is rotated and scaled, and at the right position, now we're drawing strictly in world coordinates

            context.DrawEllipse(Brushes.White, _pen, new Point(0.0d, 0.0d), 50.0d, 50.0d);
            context.DrawLine(_pen, new Point(-25.0d, -5.0d), new Point(-25.0d, 15.0d));
            context.DrawLine(_pen, new Point(25.0d, -5.0d), new Point(25.0d, 15.0d));
            context.DrawGeometry(null, _pen, _smileGeometry);

            Point cursorInWorldPoint = UIPointToWorldPoint(_cursorPoint, ViewportCenterX, ViewportCenterY, Scale, Rotation);
            context.DrawEllipse(Brushes.Gray, _pen, cursorInWorldPoint, 20.0d, 20.0d);
            

            for (int i = 0; i < 10; i++)
            {
                double orbitRadius = i * 100 + 200;
                var orbitInput = ((_timeKeeper.Elapsed.TotalMilliseconds + 987654d) / orbitRadius) / 10.0d;
                if (i % 3 == 0)
                    orbitInput *= -1;
                Point orbitPosition = new Point(Math.Sin(orbitInput) * orbitRadius, Math.Cos(orbitInput) * orbitRadius);
                context.DrawEllipse(Brushes.Gray, _pen, orbitPosition, 20.0d, 20.0d);
            }


            // end drawing the world 

            mapPositionModifier.Dispose();

            scaleModifier.Dispose();

            rotationModifier.Dispose();
            translateModifier.Dispose();

            // this is prime time to draw gui stuff 

            context.DrawLine(_pen, _cursorPoint + new Vector(-20, 0), _cursorPoint + new Vector(20, 0));
            context.DrawLine(_pen, _cursorPoint + new Vector(0, -20), _cursorPoint + new Vector(0, 20));

            clip.Dispose();

            // oh and draw again when you can, no rush, right? 
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }

        private Point UIPointToWorldPoint(Point inPoint, double viewportCenterX, double viewportCenterY, double scale, double rotation)
        {
            Point workingPoint = new Point(inPoint.X, -inPoint.Y);
            workingPoint += new Vector(-this.Bounds.Width / 2.0d, this.Bounds.Height / 2.0d);
            workingPoint /= scale;

            workingPoint = Matrix.CreateRotation(rotation).Transform(workingPoint);

            workingPoint += new Vector(viewportCenterX, viewportCenterY);

            return workingPoint;
        }

        private Point WorldPointToUIPoint(Point inPoint, double viewportCenterX, double viewportCenterY, double scale, double rotation)
        {
            Point workingPoint = new Point(inPoint.X, inPoint.Y);

            workingPoint -= new Vector(viewportCenterX, viewportCenterY);
            // undo rotation
            workingPoint = Matrix.CreateRotation(-rotation).Transform(workingPoint);
            workingPoint *= scale;
            workingPoint -= new Vector(-this.Bounds.Width / 2.0d, this.Bounds.Height / 2.0d);
            workingPoint = new Point(workingPoint.X, -workingPoint.Y);

            return workingPoint;
        }

    }
}
