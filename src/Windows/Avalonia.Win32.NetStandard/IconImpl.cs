using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class IconImpl : IWindowIconImpl
    {
        public  IntPtr HIcon { get; set; }
        public void Save(Stream outputStream)
        {
            throw new NotImplementedException();
        }
    }
}
