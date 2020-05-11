using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;

namespace ControlCatalog
{
    public class TestControl : Control
    {
        private Random _r = new Random();

        private double angle = Math.PI / 8;
        public TestControl()
        {
            DispatcherTimer t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(0.0125);

            t.Tick += (sender, e) =>
            {
                angle += Math.PI / 360;
                Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
            };

            t.Start();
        }



        public override void Render(DrawingContext drawingContext)
        {
            var lineLength = Math.Sqrt((100 * 100) + (100 * 100));

            var diffX = LineBoundsHelper.CalculateAdjSide(angle, lineLength);
            var diffY = LineBoundsHelper.CalculateOppSide(angle, lineLength);


            var p1 = new Point(400, 400);
            var p2 = new Point(p1.X + diffX, p1.Y + diffY);

            var pen = new Pen(Brushes.Green, 20, lineCap: PenLineCap.Square);
            var boundPen = new Pen(Brushes.Black);

            drawingContext.DrawLine(pen, p1, p2);

            drawingContext.DrawRectangle(boundPen, LineBoundsHelper.CalculateBounds(p1, p2, pen));

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
