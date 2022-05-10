#if DESKTOP
using System.IO;
using System.Diagnostics;
using Avalonia.Platform;

namespace NativeEmbedSample;

public partial class EmbedSample
{
    private Process _mplayer;

    IPlatformHandle CreateLinux(IPlatformHandle parent)
    {
        if (IsSecond)
        {
            var chooser = GtkHelper.CreateGtkFileChooser(parent.Handle);
            if (chooser != null)
                return chooser;
        }

        var control = base.CreateNativeControlCore(parent);
        var nodes = Path.GetFullPath(Path.Combine(typeof(EmbedSample).Assembly.GetModules()[0].FullyQualifiedName,
            "..",
            "nodes.mp4"));
        _mplayer = Process.Start(new ProcessStartInfo("mplayer",
            $"-vo x11 -zoom -loop 0 -wid {control.Handle.ToInt64()} \"{nodes}\"")
        {
            UseShellExecute = false,

        });
        return control;
    }

    void DestroyLinux(IPlatformHandle handle)
    {
        _mplayer?.Kill();
        _mplayer = null;
        base.DestroyNativeControlCore(handle);
    }
}
#endif
