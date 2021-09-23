using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.FreeDesktop.DBusSystemTray;
using Avalonia.Platform;
using Tmds.DBus;

namespace Avalonia.X11
{
    internal class X11TrayIconImpl : ITrayIconImpl
    {
        private DBusSysTray _dBusSysTray;
        private readonly ObjectPath _dbusmenuPath;

        public INativeMenuExporter MenuExporter { get; }
        public Action OnClicked { get; set; }


        public X11TrayIconImpl()
        {
            var con = DBusHelper.TryGetConnection();
            
            _dbusmenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;
            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(_dbusmenuPath, con);

            _dBusSysTray = new DBusSysTray(con);
            _dBusSysTray.Initialize(_dbusmenuPath);

            _dBusSysTray.SetActivationDelegate(() =>
            {
                OnClicked?.Invoke();
            });
        }

        public void Dispose()
        {
            _dBusSysTray?.Dispose();
        }

        public void SetIcon(IWindowIconImpl icon)
        {
            if (!(icon is X11IconData x11icon)) return;
            
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
        }

        public void SetIsVisible(bool visible)
        {
            if (visible)
            {
                // _dBusSysTray = new DBusSysTray();
                // _dBusSysTray.Initialize(_dbusmenuPath);
            }
            else
            {
                // _dBusSysTray?.Dispose();
            }
        }

        public void SetToolTipText(string text)
        {
            _dBusSysTray.SetTitleAndTooltip(text);
        }
    }
}
