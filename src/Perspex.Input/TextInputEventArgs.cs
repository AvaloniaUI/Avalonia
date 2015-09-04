using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Interactivity;

namespace Perspex.Input
{
    public class TextInputEventArgs : RoutedEventArgs
    {
        public IKeyboardDevice Device { get; set; }

        public string Text { get; set; }
    }
}
