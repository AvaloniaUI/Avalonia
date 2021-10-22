using System;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Platform;

namespace Avalonia.X11
{
    internal class XEmbedTrayIconImpl
    {
        public XEmbedTrayIconImpl()
        {
        }

        private bool IsCalled;

        private void NotImplemented()
        {
            if(IsCalled) return;
            
            Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)
                ?.Log(this,
                    "TODO: XEmbed System Tray Icons is not implemented yet. Tray icons won't be available on this system.");

            IsCalled = true;
        }
        
        public void Dispose()
        {
            NotImplemented();
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
             NotImplemented();
        }

        public void SetToolTipText(string? text)
        {
            NotImplemented();
        }

        public void SetIsVisible(bool visible)
        {
            NotImplemented();
        }

        public INativeMenuExporter? MenuExporter { get; }
        public Action? OnClicked { get; set; }
    }
}
