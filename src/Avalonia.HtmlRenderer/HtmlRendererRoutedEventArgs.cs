using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Interactivity;

namespace Avalonia.Controls.Html
{
    public class HtmlRendererRoutedEventArgs<T> : RoutedEventArgs
    {
        public T Event { get; set; }
    }
}
