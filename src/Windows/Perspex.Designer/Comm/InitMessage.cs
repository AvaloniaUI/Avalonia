using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex.Designer.Comm
{
    [Serializable]
    class InitMessage : UpdateXamlMessage
    {
        public string TargetExe { get; private set; }
        public InitMessage(string targetExe, string xaml) : base(xaml)
        {
            TargetExe = targetExe;
        }
    }
}
