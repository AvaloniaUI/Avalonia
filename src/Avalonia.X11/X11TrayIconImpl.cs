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
            _dBusTrayIcon = new DBusTrayIconImpl();
        }

        private readonly DBusTrayIconImpl _dBusTrayIcon;

        private readonly XEmbedTrayIconImpl _xEmbedTrayIcon;
        private bool _isDisposed;

        public void Dispose()
        {
            _dBusTrayIcon?.Dispose();
            _xEmbedTrayIcon?.Dispose();
            _isDisposed = true;
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed) return;

            if (_dBusTrayIcon?.IsActive ?? false)
            {
                if (!(icon is X11IconData x11icon))
                    return;

                _dBusTrayIcon.SetIcon(x11icon.Data);
            }
            else
            {
                _xEmbedTrayIcon.SetIcon(icon);
            }
        }

        public void SetToolTipText(string? text)
        {
            if (_isDisposed) return;

            if (_dBusTrayIcon?.IsActive ?? false)
            {
                _dBusTrayIcon.SetToolTipText(text);
            }
            else
            {
                _xEmbedTrayIcon.SetToolTipText(text);
            }
        }

        public void SetIsVisible(bool visible)
        {
            if (_isDisposed) return;

            if (_dBusTrayIcon?.IsActive ?? false)
            {
                _dBusTrayIcon.SetIsVisible(visible);
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
                if (_dBusTrayIcon?.IsActive ?? false)
                {
                    return _dBusTrayIcon.MenuExporter;
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
                if (_dBusTrayIcon?.IsActive ?? false)
                {
                    return _dBusTrayIcon.OnClicked;
                }
                else
                {
                    return _xEmbedTrayIcon.OnClicked;
                }
            }
            set
            {
                if (_dBusTrayIcon?.IsActive ?? false)
                {
                     _dBusTrayIcon.OnClicked = value;
                }
                else
                {
                    _xEmbedTrayIcon.OnClicked = value;
                }
            }
        }
    }
}
