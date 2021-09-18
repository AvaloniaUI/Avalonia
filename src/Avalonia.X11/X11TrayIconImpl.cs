using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop.DBusSystemTray;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class X11TrayIconImpl : ITrayIconImpl
    {
        public INativeMenuExporter MenuExporter { get; }
        public Action OnClicked { get; set; }

        private DBusSysTray dBusSysTray;

        public X11TrayIconImpl()
        {
            dBusSysTray = new DBusSysTray();
            dBusSysTray.Initialize();
            MenuExporter = dBusSysTray.NativeMenuExporter;
        }

        public void Dispose()
        {
            dBusSysTray?.Dispose();
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

            dBusSysTray.SetIcon(new Pixmap(w, h, pixByteArray));
        }

        public void SetIsVisible(bool visible)
        {
            
        }

        public void SetToolTipText(string text)
        {
        }
    }
}
