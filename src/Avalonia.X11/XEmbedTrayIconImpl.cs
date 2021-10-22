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
            Logger.TryGet(LogEventLevel.Error, LogArea.X11Platform)
                ?.Log(this,
                    "TODO: XEmbed System Tray Icons is not implemented yet. Tray icons won't be available on this system.");
        }

        public void Dispose()
        {
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
        }

        public void SetToolTipText(string? text)
        {
        }

        public void SetIsVisible(bool visible)
        {
        }

        public INativeMenuExporter? MenuExporter { get; }
        public Action? OnClicked { get; set; }
    }
}
