using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;
using static Avalonia.Controls.Platform.IInsetsManager;

namespace Avalonia.Browser
{
    internal class BrowserInsetsManager : IInsetsManager
    {
        public SystemBarTheme? SystemBarTheme { get; set; }
        public bool? IsSystemBarVisible
        {
            get
            {
                return DomHelper.IsFullscreen();
            }
            set
            {
                DomHelper.SetFullscreen(value != null ? !value.Value : false);
            }
        }

        public bool DisplayEdgeToEdge { get; set; }

        public event EventHandler<SafeAreaChangedArgs>? SafeAreaChanged;

        public Thickness GetSafeAreaPadding()
        {
            var padding = DomHelper.GetSafeAreaPadding();

            return new Thickness(padding[0], padding[1], padding[2], padding[3]);
        }

        public void NotifySafeAreaPaddingChanged()
        {
            SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(GetSafeAreaPadding()));
        }
    }
}
