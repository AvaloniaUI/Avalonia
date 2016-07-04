using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.Comm
{
    [Serializable]
    class InitMessage : UpdateXamlMessage
    {
        public string TargetExe { get; private set; }
        public InitMessage(string targetExe, string xaml, string sourceAssembly) : base(xaml, sourceAssembly)
        {
            TargetExe = targetExe;
        }
    }
}
