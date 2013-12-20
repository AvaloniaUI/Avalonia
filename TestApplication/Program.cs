using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex;
using Perspex.Controls;
using Perspex.Media;
using Perspex.Windows;
using Perspex.Windows.Media;
using Perspex.Windows.Threading;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceLocator.Register<ITextService>(() => new TextService(new SharpDX.DirectWrite.Factory()));

            Window window = new Window();

            window.Content = new Button
            {
                Content = "Hello World",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Background = new SolidColorBrush(0xff808080),
                BorderThickness = 2,
                BorderBrush = new SolidColorBrush(0xff000000),
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
