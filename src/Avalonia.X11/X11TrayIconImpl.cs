using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.FreeDesktop.DBusSystemTray;
using Avalonia.Platform;

namespace Avalonia.X11
{
   internal class X11TrayIconImpl : ITrayIconImpl
   {
        
        private readonly DBusSysTray _dBusSysTray;
        private X11IconData lastIcon;
        
        public INativeMenuExporter MenuExporter { get; }
        public Action OnClicked { get; set; }

        
        public X11TrayIconImpl()
        {
            _dBusSysTray = new DBusSysTray();
            
            
            var dbusmenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;
            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(dbusmenuPath);

            
            _dBusSysTray.Initialize(dbusmenuPath);
            
        }

        public void Dispose()
        {
            _dBusSysTray?.Dispose();
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (!(icon is X11IconData x11icon)) return;

            lastIcon = x11icon;

            var w = (int)x11icon.Data[0];
            var h = (int)x11icon.Data[1];

            var rx = x11icon.Data.AsSpan(2);
            var pixLength = w * h;

            var pixByteArrayCounter = 0;
            var pixByteArray = new byte[w * h * 4];

            for (var i = 0; i < pixLength; i++)
            {
                var u = rx[i].ToUInt32();
                pixByteArray[pixByteArrayCounter++] = (byte)((u & 0xFF000000) >> 24);
                pixByteArray[pixByteArrayCounter++] = (byte)((u & 0xFF0000) >> 16);
                pixByteArray[pixByteArrayCounter++] = (byte)((u & 0xFF00) >> 8);
                pixByteArray[pixByteArrayCounter++] = (byte)(u & 0xFF);
            }

            _dBusSysTray.SetIcon(new DbusPixmap(w, h, pixByteArray));

            _dBusSysTray.SetActivationDelegate(() =>
            {
                OnClicked?.Invoke();
            });
        }

        public void SetIsVisible(bool visible)
        {
            if (visible && lastIcon != null)
            {
                SetIcon(lastIcon);
            }
            else
            {
                _dBusSysTray.SetIcon(new DbusPixmap(1, 1, new byte[] { 0, 0, 0, 0 }));
            }
        }

        public void SetToolTipText(string text)
        {
            _dBusSysTray.SetTitleAndTooltip(text);
        }
    }
}
