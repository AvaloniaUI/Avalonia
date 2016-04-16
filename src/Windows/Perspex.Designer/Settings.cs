using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Perspex.Designer
{
    static class Settings
    {
        static readonly string Root = @"HKEY_CURRENT_USER\Software\PerspexUI\Designer";
        public static string Background
        {
            get { return Registry.GetValue(Root, "Background", "#f3f3f3")?.ToString() ?? "#f3f3f3"; }
            set { Registry.SetValue(Root, "Background", value); }
        }
    }
}
