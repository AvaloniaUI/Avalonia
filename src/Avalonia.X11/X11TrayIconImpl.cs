using System;
using Avalonia.Controls.Platform;
using Avalonia.Platform;

namespace Avalonia.X11
{
    class X11TrayIconImpl : ITrayIconImpl
    {
        public INativeMenuExporter MenuExporter => null;

        public Action OnClicked { get; set; }

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
            
        }
    }
}
