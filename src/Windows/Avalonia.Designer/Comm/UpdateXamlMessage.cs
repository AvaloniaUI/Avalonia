using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.Comm
{
    [Serializable]
    class UpdateXamlMessage
    {
        public UpdateXamlMessage(string xaml, string assemblyPath)
        {
            Xaml = xaml;
            AssemblyPath = assemblyPath;
        }

        public string Xaml { get; private set; }
        public string AssemblyPath { get; private set; }
    }
}
