using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    // ReSharper disable InconsistentNaming

    public class AcrylicPage : UserControl
    {
        public static readonly StyledProperty<int> RadiusProperty =
            AvaloniaProperty.Register<AcrylicPage, int>(nameof(Radius), 25);
        
        public static readonly StyledProperty<IBrush> SegmentColorProperty = 
            AvaloniaProperty.Register<AcrylicPage, IBrush>(nameof(SegmentColor), Brushes.Red);
        
        public static readonly StyledProperty<int> StrokeThicknessProperty = 
            AvaloniaProperty.Register<AcrylicPage, int>(nameof(StrokeThickness), 15);
        
        public static readonly StyledProperty<double> PercentageProperty = 
            AvaloniaProperty.Register<AcrylicPage, double>(nameof(Percentage),65);
        
        public static readonly StyledProperty<double> AngleProperty = 
            AvaloniaProperty.Register<AcrylicPage, double>(nameof(Angle),120);

        private int _PathFigureWidth;
        public static readonly DirectProperty<AcrylicPage, int> PathFigureWidthProperty = 
            AvaloniaProperty.RegisterDirect<AcrylicPage, int>(nameof(PathFigureWidth), o => o.PathFigureWidth, (o, v) => o.PathFigureWidth = v);
        
        private int _PathFigureHeight;
        public static readonly DirectProperty<AcrylicPage, int> PathFigureHeightProperty = 
            AvaloniaProperty.RegisterDirect<AcrylicPage, int>(nameof(PathFigureHeight), o => o.PathFigureHeight, (o, v) => o.PathFigureHeight = v);
        
        private Thickness _PathFigureMargin;
        public static readonly DirectProperty<AcrylicPage, Thickness> PathFigureMarginProperty =
            AvaloniaProperty.RegisterDirect<AcrylicPage, Thickness>(nameof(PathFigureMargin), o => o.PathFigureMargin, (o, v) => o.PathFigureMargin = v);
        
        
        private Point _PathFigureStartPoint;
        public static readonly DirectProperty<AcrylicPage, Point> PathFigureStartPointProperty = 
            AvaloniaProperty.RegisterDirect<AcrylicPage, Point>(nameof(PathFigureStartPoint), o => o.PathFigureStartPoint, (o, v) => o.PathFigureStartPoint = v);

        
        private Point _ArcSegmentPoint;
        public static readonly DirectProperty<AcrylicPage, Point> ArcSegmentPointProperty = 
            AvaloniaProperty.RegisterDirect<AcrylicPage, Point>(nameof(ArcSegmentPoint), o => o.ArcSegmentPoint, (o, v) => o.ArcSegmentPoint = v);
        
        private Size _ArcSegmentSize;
        public static readonly DirectProperty<AcrylicPage, Size> ArcSegmentSizeProperty =
            AvaloniaProperty.RegisterDirect<AcrylicPage, Size>(nameof(ArcSegmentSize), o => o.ArcSegmentSize, (o, v) => o.ArcSegmentSize = v);
        
        private bool _ArcSegmentIsLargeArc;
        public static readonly DirectProperty<AcrylicPage, bool> ArcSegmentIsLargeArcProperty = 
            AvaloniaProperty.RegisterDirect<AcrylicPage, bool>(nameof(ArcSegmentIsLargeArc), o => o.ArcSegmentIsLargeArc, (o, v) => o.ArcSegmentIsLargeArc = v);
        
        public AcrylicPage()
        {
            InitializeComponent();

            var kl = new Random();
            this.InitializeComponent();
            this.DataContext = this;
            Task.Factory.StartNew(() =>
            {
                double x = 0;
                while (true)
                {
                    x += 1;
                    x %= 101;
                    Dispatcher.UIThread.Post(() =>
                    {
                        Percentage = x;
                        Console.WriteLine(x);
                    });
                    
                    Thread.Sleep(50);
                }
            });
        }

        public int Radius
        {
            get { return GetValue(RadiusProperty); }
            set { SetValue(RadiusProperty, value); }
        }

        public IBrush SegmentColor
        {
            get { return GetValue(SegmentColorProperty); }
            set { SetValue(SegmentColorProperty, value); }
        }

        public int StrokeThickness
        {
            get { return GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        public double Percentage
        {
            get { return GetValue(PercentageProperty); }
            set { SetValue(PercentageProperty, value); }
        }

        public double Angle
        {
            get { return GetValue(AngleProperty); }
            set { SetValue(AngleProperty, value); }
        }

        public int PathFigureWidth
        {
            get { return _PathFigureWidth; }
            set { SetAndRaise(PathFigureWidthProperty, ref _PathFigureWidth, value); }
        }

        public int PathFigureHeight
        {
            get { return _PathFigureHeight; }
            set { SetAndRaise(PathFigureHeightProperty, ref _PathFigureHeight, value); }
        }

        public Thickness PathFigureMargin
        {
            get { return _PathFigureMargin; }
            set { SetAndRaise(PathFigureMarginProperty, ref _PathFigureMargin, value); }
        }

        public Point PathFigureStartPoint
        {
            get { return _PathFigureStartPoint; }
            set { SetAndRaise(PathFigureStartPointProperty, ref _PathFigureStartPoint, value); }
        }

        public Point ArcSegmentPoint
        {
            get { return _ArcSegmentPoint; }
            set { SetAndRaise(ArcSegmentPointProperty, ref _ArcSegmentPoint, value); }
        }

        public Size ArcSegmentSize
        {
            get { return _ArcSegmentSize; }
            set { SetAndRaise(ArcSegmentSizeProperty, ref _ArcSegmentSize, value); }
        }

        public bool ArcSegmentIsLargeArc
        {
            get { return _ArcSegmentIsLargeArc; }
            set { SetAndRaise(ArcSegmentIsLargeArcProperty, ref _ArcSegmentIsLargeArc, value); }
        }


        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            Angle = (Percentage * 360) / 100;
            RenderArc();
        }

        public void RenderArc()
        {
            var startPoint =  new Point(Radius, 0);
            var endPoint = ComputeCartesianCoordinate(Angle, Radius);
            endPoint += new Point(Radius, Radius);
        
            PathFigureWidth = Radius * 2 + StrokeThickness;
            PathFigureHeight = Radius * 2 + StrokeThickness;
            PathFigureMargin = new Thickness(StrokeThickness, StrokeThickness, 0, 0);

            var largeArc = Angle > 180.0;

            var outerArcSize = new Size(Radius, Radius);

            PathFigureStartPoint = startPoint;

            if (Math.Abs(startPoint.X - Math.Round(endPoint.X)) < 0.01 && Math.Abs(startPoint.Y - Math.Round(endPoint.Y)) < 0.01)
                endPoint -= new Point(0.01,0);

            ArcSegmentPoint = endPoint;
            ArcSegmentSize = outerArcSize;
            ArcSegmentIsLargeArc = largeArc;
        }

        private Point ComputeCartesianCoordinate(double angle, double radius)
        {
            // convert to radians
            var angleRad = (Math.PI / 180.0) * (angle - 90);

            var x = radius * Math.Cos(angleRad);
            var y = radius * Math.Sin(angleRad);

            return new Point(x, y);
        }
        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
