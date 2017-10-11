using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Designer.HostApp.NetFX
{
    class Program
    {
        public static void Main(string[] args)
            => Avalonia.DesignerSupport.Remote.RemoteDesignerEntryPoint.Main(args);
    }
}
