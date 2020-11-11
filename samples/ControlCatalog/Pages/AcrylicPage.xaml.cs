using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace ControlCatalog.Pages
{
    public class AcrylicPage : UserControl
    {
        
        /// <summary>
        /// Defines the <see cref="Point"/> property.
        /// </summary>
        public static readonly StyledProperty<Point> PointProperty
            = AvaloniaProperty.Register<AcrylicPage, Point>(nameof(Point));

        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>
        /// The point.
        /// </value>
        public Point Point
        {
            get { return GetValue(PointProperty); }
            set { SetValue(PointProperty, value); }
        }

        public AcrylicPage()
        {
            var kl = new Random();
            this.InitializeComponent();
            this.DataContext = this;
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Thread.Sleep(500);
                    SetBarLength(kl.NextDouble());
                    
                }
            });
        }

        public void SetBarLength(double Value)
        {
            double Angle = 2 * 3.14159265 * Value;

            double X = 100 - Math.Sin(Angle) * 100;
            double Y = 100 + Math.Cos(Angle) * 100;

            if (Value > 0 && (int)X == 100 && (int)Y == 100)
                X += 0.01; // Never make the end the same as the start!

            Dispatcher.UIThread.Post(() =>
            {
                Point = new Point(X, Y);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
