using System.IO;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia.Controls.Platform;
using System;
using ControlCatalog.Pages;

namespace ControlCatalog.NetCore;

public class EmbedSampleGtk : INativeDemoControl
{
    private Process _mplayer;

    public IPlatformHandle CreateControl(bool isSecond, IPlatformHandle parent, Func<IPlatformHandle> createDefault)
    {
        if (isSecond)
        {
            var chooser = GtkHelper.CreateGtkFileChooser(parent.Handle);
            if (chooser != null)
                return chooser;
        }

        var control = createDefault();
        var nodes = Path.GetFullPath(Path.Combine(typeof(EmbedSample).Assembly.GetModules()[0].FullyQualifiedName,
            "..", "NativeControls", "Gtk", "nodes.mp4"));
        _mplayer = Process.Start(new ProcessStartInfo("mplayer",
            $"-vo x11 -zoom -loop 0 -wid {control.Handle.ToInt64()} \"{nodes}\"")
        {
            UseShellExecute = false,

        });
        return control;
    }
}
