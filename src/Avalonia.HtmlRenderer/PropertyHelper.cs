using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.HtmlRenderer
{
    static class PropertyHelper
    {

        public static AvaloniaProperty Register<TOwner, T>(string name, T def, Action<AvaloniaObject, AvaloniaPropertyChangedEventArgs> changed) where TOwner : AvaloniaObject
        {
            var pp = AvaloniaProperty.Register<TOwner, T>(name, def);
            Action<AvaloniaPropertyChangedEventArgs> cb = args =>
            {
                changed(args.Sender, args);
            };

            pp.Changed.Subscribe(cb);
            return pp;
        }
    }
}
