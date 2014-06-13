using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex;
using Perspex.Controls;
using Perspex.Input;
using Perspex.Media;
using Perspex.Shapes;
using Perspex.Styling;
using Perspex.Themes.Default;
using Perspex.Windows;
using Perspex.Windows.Threading;
using Splat;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            App application = new App();

            Window window = new Window
            {
                Content = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Orientation = Orientation.Vertical,
                    Gap = 6,
                    Children = new PerspexList<Control>
                    {
                        new Button
                        {
                            Content = "Button",
                        },
                        new Button
                        {
                            Content = "Explict Background",
                            Background = new SolidColorBrush(0xffa0a0ff),
                        },
                        new CheckBox
                        {
                            Content = "Checkbox",
                        },
                    }
                }
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
