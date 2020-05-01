using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.WebKit;
using Encoding = SharpDX.Text.Encoding;

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
            _mplayer?.Kill();
            _mplayer = null;
            base.DestroyNativeControlCore(handle);
        }

        private const string RichText =
            @"{\rtf1\ansi\ansicpg1251\deff0\nouicompat\deflang1049{\fonttbl{\f0\fnil\fcharset0 Calibri;}}
{\colortbl ;\red255\green0\blue0;\red0\green77\blue187;\red0\green176\blue80;\red155\green0\blue211;\red247\green150\blue70;\red75\green172\blue198;}
{\*\generator Riched20 6.3.9600}\viewkind4\uc1 
\pard\sa200\sl276\slmult1\f0\fs22\lang9 <PREFIX>I \i am\i0  a \cf1\b Rich Text \cf0\b0\fs24 control\cf2\fs28 !\cf3\fs32 !\cf4\fs36 !\cf1\fs40 !\cf5\fs44 !\cf6\fs48 !\cf0\fs44\par
}";

        IPlatformHandle CreateWin32(IPlatformHandle parent)
        {
            WinApi.LoadLibrary("Msftedit.dll");
            var handle = WinApi.CreateWindowEx(0, "RICHEDIT50W",
                @"Rich Edit",
                0x800000 | 0x10000000 | 0x40000000 | 0x800000 | 0x10000 | 0x0004, 0, 0, 1, 1, parent.Handle,
                IntPtr.Zero, WinApi.GetModuleHandle(null), IntPtr.Zero);
            var st = new WinApi.SETTEXTEX { Codepage = 65001, Flags = 0x00000008 };
            var text = RichText.Replace("<PREFIX>", IsSecond ? "\\qr " : "");
            var bytes = Encoding.UTF8.GetBytes(text);
            WinApi.SendMessage(handle, 0x0400 + 97, ref st, bytes);
            return new PlatformHandle(handle, "HWND");

        }

        void DestroyWin32(IPlatformHandle handle)
        {
            WinApi.DestroyWindow(handle.Handle);
        }

        IPlatformHandle CreateOSX(IPlatformHandle parent)
        {
            // Note: We are using MonoMac for example purposes
            // It shouldn't be used in production apps
            MacHelper.EnsureInitialized();

            var webView = new WebView();
            Dispatcher.UIThread.Post(() =>
            {
                webView.MainFrame.LoadRequest(new NSUrlRequest(new NSUrl(
                    IsSecond ? "https://bing.com": "https://google.com/")));
            });
            return new MacOSViewHandle(webView);

        }

        void DestroyOSX(IPlatformHandle handle)
        {
            ((MacOSViewHandle)handle).Dispose();
        }
        
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return CreateLinux(parent);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateWin32(parent);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return CreateOSX(parent);
            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DestroyLinux(control);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DestroyWin32(control);
            else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                DestroyOSX(control);
            else
                base.DestroyNativeControlCore(control);
        }
    }
}
