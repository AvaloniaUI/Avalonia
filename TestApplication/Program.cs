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
using Perspex.Styling;
using Perspex.Themes.Default;
using Perspex.Windows;
using Perspex.Windows.Media;
using Perspex.Windows.Threading;
using Splat;

namespace TestApplication
{
    class TestLogger : ILogger
    {
        public LogLevel Level
        {
            get;
            set;
        }

        public void Write(string message, LogLevel logLevel)
        {
            if ((int)logLevel < (int)Level) return;
            System.Diagnostics.Debug.WriteLine(message);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            TextService textService = new TextService(new SharpDX.DirectWrite.Factory());

            Locator.CurrentMutable.Register(() => textService, typeof(ITextService));
            Locator.CurrentMutable.Register(() => new Styler(), typeof(IStyler));
            Locator.CurrentMutable.Register(() => new TestLogger(), typeof(ILogger));

            Application application = new Application
            {
                Styles = new Styles
                {
                    new DefaultTheme(),
                }
            };

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
                        }
                    }
                }
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
