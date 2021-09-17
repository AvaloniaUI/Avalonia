using System;
using System.Linq;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.DBusSystemTray;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class X11TrayIconImpl : ITrayIconImpl
    {
        private readonly AvaloniaX11Platform _avaloniaX11Platform;
        public INativeMenuExporter MenuExporter => null;

        public Action OnClicked { get; set; }
        private SNIDBus sni = new SNIDBus();

        public X11TrayIconImpl(AvaloniaX11Platform avaloniaX11Platform)
        {
            _avaloniaX11Platform = avaloniaX11Platform;
         }

        public void Dispose()
        {
            
            
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            
        }

        public void SetIsVisible(bool visible)
        {
            
        }

        public void SetToolTipText(string text)
        {
             sni.Initialize();
        }
    }
}
