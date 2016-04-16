using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Designer.Comm
{
    [Serializable]
    class UpdateXamlMessage
    {
        public UpdateXamlMessage(string xaml)
        {
            Xaml = xaml;
        }

        public string Xaml { get; private set; }
    }
}
