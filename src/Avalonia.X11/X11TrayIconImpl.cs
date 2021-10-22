using System;
using Avalonia.Controls.Platform;
using Avalonia.FreeDesktop;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.X11
{
    internal class X11TrayIconImpl : ITrayIconImpl
    {
        public X11TrayIconImpl()
        {
            _xEmbedTrayIcon = new XEmbedTrayIconImpl();

            var _connection = DBusHelper.TryGetConnection();

            if (_connection is null)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)
                    ?.Log(this, "Unable to get a dbus connection for system tray icons.");
            }

            _dbusSniTrayIcon = new DbusSNITrayIconImpl(_connection);
        }

        private readonly DbusSNITrayIconImpl _dbusSniTrayIcon;

        private readonly XEmbedTrayIconImpl _xEmbedTrayIcon;
        private bool _isDisposed;

        public void Dispose()
        {
            _dbusSniTrayIcon?.Dispose();
            _xEmbedTrayIcon?.Dispose();
            _isDisposed = true;
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed) return;

            if (_dbusSniTrayIcon?.IsActive ?? false)
            {
                if (!(icon is X11IconData x11icon))
                    return;

                _dbusSniTrayIcon.SetIcon(x11icon.Data);
            }
            else
            {
                _xEmbedTrayIcon.SetIcon(icon);
            }
        }

        public void SetToolTipText(string? text)
        {
            if (_isDisposed) return;

            if (_dbusSniTrayIcon?.IsActive ?? false)
            {
                _dbusSniTrayIcon.SetToolTipText(text);
            }
            else
            {
                _xEmbedTrayIcon.SetToolTipText(text);
            }
        }

        public void SetIsVisible(bool visible)
        {
            if (_isDisposed) return;

            if (_dbusSniTrayIcon?.IsActive ?? false)
            {
                _dbusSniTrayIcon.SetIsVisible(visible);
            }
            else
            {
                _xEmbedTrayIcon.SetIsVisible(visible);
            }
        }

        public INativeMenuExporter? MenuExporter
        {
            get
            {
                if (_dbusSniTrayIcon?.IsActive ?? false)
                {
                    return _dbusSniTrayIcon.MenuExporter;
                }
                else
                {
                    return _xEmbedTrayIcon.MenuExporter;
                }
            }
        }

        public Action? OnClicked
        {
            get
            {
                if (_dbusSniTrayIcon?.IsActive ?? false)
                {
                    return _dbusSniTrayIcon.OnClicked;
                }
                else
                {
                    return _xEmbedTrayIcon.OnClicked;
                }
            }
            set
            {
                if (_dbusSniTrayIcon?.IsActive ?? false)
                {
                     _dbusSniTrayIcon.OnClicked = value;
                }
                else
                {
                    _xEmbedTrayIcon.OnClicked = value;
                }
            }
        }
    }
}
