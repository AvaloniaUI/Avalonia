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
            Locator.CurrentMutable.Register(() => new TextService(new SharpDX.DirectWrite.Factory()), typeof(ITextService));
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
                Content = new TestBorder 
                { 
                    Styles = new Styles 
                    { 
                        new Style(new Selector().OfType<TestBorder>())
                        {
                            Setters = new[]
                            {
                                new Setter(TestBorder.BackgroundProperty, new SolidColorBrush(0xff0000ff)),
                            }
                        },
                        new Style(new Selector().OfType<TestBorder>().Class(":mouseover"))
                        {
                            Setters = new[]
                            {
                                new Setter(TestBorder.BackgroundProperty, new SolidColorBrush(0xffff0000)),
                            }
                        },
                    }
                },

                //Content = new Button
                //{
                //    Content = "Hello World",
                //    HorizontalAlignment = HorizontalAlignment.Center,
                //    VerticalAlignment = VerticalAlignment.Center,
                //},
            };

            window.Show();
            Dispatcher.Run();
        }
    }
}
