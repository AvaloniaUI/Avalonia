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

        public override void Render(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Brushes.Black, 10), new Point(0, 100), new Point(Bounds.Width * _r.NextDouble(), 100));
            drawingContext.DrawLine(new Pen(Brushes.Black, 10), new Point(0, 150), new Point(Bounds.Width * _r.NextDouble(), 100));
            drawingContext.DrawLine(new Pen(Brushes.Black, 10), new Point(0, 100), new Point(Bounds.Width * double.NegativeInfinity, 150));            
            Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Background);
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
