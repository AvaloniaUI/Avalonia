using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;

namespace NativeEmbedSample
{
    public class EmbedSample : NativeControlHost
    {
        public bool IsSecond { get; set; }
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
            _mplayer.Kill();
            _mplayer = null;
            base.DestroyNativeControlCore(handle);
        }
        
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return CreateLinux(parent);
            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DestroyLinux(control);
            else
                base.DestroyNativeControlCore(control);
        }
    }
}
