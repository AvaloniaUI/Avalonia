using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

            Application application = new Application
            {
                Styles = new Styles 
                {
                    new Style(x => x.Select<Button>())
                    {
                        Setters = new[]
                        {
                            new Setter(Button.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                            new Setter(Button.BorderBrushProperty, new SolidColorBrush(0xff707070)),
                            new Setter(Button.BorderThicknessProperty, 1),
                            new Setter(Button.ForegroundProperty, new SolidColorBrush(0xff000000)),
                        },
                    },
                    new Style(x => x.Select<Button>().Class(":mouseover"))
                    {
                        Setters = new[]
                        {
                            new Setter (Button.BackgroundProperty, new SolidColorBrush(0xffbee6fd)),
                            new Setter (Button.BorderBrushProperty, new SolidColorBrush(0xff3c7fb1)),
                        },
                    }
                }
            };

            Window window = new Window
            {
                Content = new Button
                {
                    Content = "Hello World",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    BorderThickness = 2,
                    BorderBrush = new SolidColorBrush(0xff000000),
                },
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
