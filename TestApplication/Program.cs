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

            Window window = new Window
            {
                Styles = new ObservableCollection<Style>
                {
                    new Style
                    {
                        Selector = x => x.OfType<Button>(),
                        Setters = new[]
                        {
                            new Setter 
                            { 
                                Property = Button.BackgroundProperty, 
                                Value = new SolidColorBrush(0xffff8080),
                            }
                        },
                    }
                },
                Content = new Button
                {
                    Content = "Hello World",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(0xff808080),
                    BorderThickness = 2,
                    BorderBrush = new SolidColorBrush(0xff000000),
                },
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
