using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Controls.Platform;
using Avalonia.Media;
using static Avalonia.Controls.Platform.IInsetsManager;

namespace Avalonia.Browser
{
    internal class BrowserInsetsManager : IInsetsManager
    {
        public bool? IsSystemBarVisible
        {
            get
            {
                return DomHelper.IsFullscreen();
            }
            set
            {
                DomHelper.SetFullscreen(!value ?? false);
            }
        }

        public bool DisplayEdgeToEdge { get; set; }

        public event EventHandler<SafeAreaChangedArgs>? SafeAreaChanged;

        public Thickness SafeAreaPadding
        {
            get
            {
                var padding = DomHelper.GetSafeAreaPadding();

                return new Thickness(padding[0], padding[1], padding[2], padding[3]);
            }
        }

        public Color? SystemBarColor { get; set; }

        public void NotifySafeAreaPaddingChanged()
        {
            SafeAreaChanged?.Invoke(this, new SafeAreaChangedArgs(SafeAreaPadding));
        }
    }
}
