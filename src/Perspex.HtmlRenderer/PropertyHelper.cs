using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.HtmlRenderer
{
    static class PropertyHelper
    {

        public static PerspexProperty Register<TOwner, T>(string name, T def, Action<PerspexObject, PerspexPropertyChangedEventArgs> changed) where TOwner : PerspexObject
        {
            var pp = PerspexProperty.Register<TOwner, T>(name, def);
            Action<PerspexPropertyChangedEventArgs> cb = args =>
            {
                changed(args.Sender, args);
            };

            pp.Changed.Subscribe(cb);
            return pp;
        }
    }
}
