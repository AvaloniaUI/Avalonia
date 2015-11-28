using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.DesignerSupport
{
    class DesignerApi
    {
        private readonly Dictionary<string, object> _inner;

        public DesignerApi(Dictionary<string, object> inner)
        {
            _inner = inner;
        }

        object Get([CallerMemberName] string name = null)
        {
            object rv;
            _inner.TryGetValue(name, out rv);
            return rv;
        }

        void Set(object value, [CallerMemberName] string name = null)
        {
            _inner[name] = value;
        }

        public Action<string> UpdateXaml
        {
            get { return (Action<string>) Get(); }
            set {Set(value); }
        }

        public Action OnResize
        {
            get { return (Action) Get(); }
            set { Set(value);}
        }

        public Action<IntPtr> OnWindowCreated
        {
            set { Set(value); }
            get { return (Action<IntPtr>) Get(); }
        }

        public Action<double> SetScalingFactor
        {
            set { Set(value);}
            get { return (Action<double>) Get(); }
        }

    }
}
