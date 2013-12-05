using System;
using System.Collections.Generic;
using System.Linq;
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

            window.Content = new TextBlock
            {
                Text = "Hello World",
                Background = new SolidColorBrush(0xffffffff),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            
            window.Show();
            Dispatcher.Run();
        }
    }
}
