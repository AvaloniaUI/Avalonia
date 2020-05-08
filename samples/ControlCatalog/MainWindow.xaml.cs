using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog
{
    public class TestControl : Control
    {
        private Random _r = new Random();

        private double angle = 45;
        public TestControl()
        {
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(0.0125);

            t.Tick += (sender, e) =>
            {
                angle += 1;
               Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            };

            t.Start();
        }

        const double degreeToRadians = Math.PI / 180.0;

        private static double CalculateAngle(Point p1, Point p2)
        {
            var xDiff = p2.X - p1.X;
            var yDiff = p2.Y - p1.Y;

            return Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
        }

        private static double CalculateOppSide(double angle, double hyp)
        {
            return Math.Sin(angle * degreeToRadians) * hyp;
        }

        private static double CalculateAdjSide(double angle, double hyp)
        {   
            return Math.Cos(angle * degreeToRadians) * hyp;
        }

        static Rect CalculateBounds(Point p1, Point p2, double thickness, double angleToCorner)
        {
            var pts = TranslatePointsAlongTangent(p1, p2, angleToCorner + 90, thickness / 2);

            return new Rect(pts.p1, pts.p2);
        }
        
        static (Point p1, Point p2) TranslatePointsAlongTangent(Point p1, Point p2, double angle, double distance)
        {
            var xDiff = CalculateOppSide(angle, distance);
            var yDiff = CalculateAdjSide(angle, distance);            

            var c1 = new Point(p1.X + xDiff, p1.Y - yDiff);
            var c2 = new Point(p1.X - xDiff, p1.Y + yDiff);

            var c3 = new Point(p2.X + xDiff, p2.Y - yDiff);
            var c4 = new Point(p2.X - xDiff, p2.Y + yDiff);

            var minX = Math.Min(c1.X, Math.Min(c2.X, Math.Min(c3.X, c4.X)));
            var minY = Math.Min(c1.Y, Math.Min(c2.Y, Math.Min(c3.Y, c4.Y)));
            var maxX = Math.Max(c1.X, Math.Max(c2.X, Math.Max(c3.X, c4.X)));
            var maxY = Math.Max(c1.Y, Math.Max(c2.Y, Math.Max(c3.Y, c4.Y)));

            return (new Point(minX, minY), new Point(maxX, maxY));            
        }

        static Rect CalculateBounds (Point p1, Point p2, Pen p)
        {
            var angle = CalculateAngle(p1, p2);

            var angleToCorner = 90 - angle;

            if (p.LineCap != PenLineCap.Flat)
            {
                var pts = TranslatePointsAlongTangent(p1, p2, angleToCorner, p.Thickness / 2);

                return CalculateBounds(pts.p1, pts.p2, p.Thickness, angleToCorner);
            }
            else
            {
                return CalculateBounds(p1, p2, p.Thickness, angleToCorner);
            }
        }

        

        public override void Render(DrawingContext drawingContext)
        {
            var lineLength = Math.Sqrt((100 * 100) + (100 * 100));

            var diffX = CalculateAdjSide(angle, lineLength);
            var diffY = CalculateOppSide(angle, lineLength);            


            var p1 = new Point(400, 400);
            var p2 = new Point(p1.X + diffX, p1.Y + diffY);

            var pen = new Pen(Brushes.Green, 20, lineCap: PenLineCap.Round);
            var boundPen = new Pen(Brushes.Black);

            drawingContext.DrawLine(pen, p1, p2);

            drawingContext.DrawRectangle(boundPen, CalculateBounds(p1, p2, pen));

            //Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
        }
    }

    public class TestControl1 : Control
    {
        private Random _r = new Random();

        public override void Render(DrawingContext drawingContext)
        {
            drawingContext.FillRectangle(Brushes.Red, Bounds);
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // TODO: iOS does not support dynamically loading assemblies
            // so we must refer to this resource DLL statically. For
            // now I am doing that here. But we need a better solution!!
            var theme = new Avalonia.Themes.Default.DefaultTheme();
            theme.TryGetResource("Button", out _);
            AvaloniaXamlLoader.Load(this);
        }
    }
}
