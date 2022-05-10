using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;

namespace NativeEmbedSample
{
    public partial class EmbedSample : NativeControlHost
    {
        public bool IsSecond { get; set; }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
#if DESKTOP
            if (OperatingSystem.IsLinux())
                return CreateLinux(parent);
            if (OperatingSystem.IsWindows())
                return CreateWin32(parent);
            if (OperatingSystem.IsMacOS())
                return CreateOSX(parent);
#elif __ANDROID__ || ANDROID
            if (OperatingSystem.IsAndroid())
                return CreateAndroid(parent);
#elif IOS
            if (OperatingSystem.IsIOS())
                return CreateIOS(parent);
#endif
            return base.CreateNativeControlCore(parent);
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
#if DESKTOP
            if (OperatingSystem.IsLinux())
                DestroyLinux(control);
            else if (OperatingSystem.IsWindows())
                DestroyWin32(control);
            else if (OperatingSystem.IsMacOS())
                DestroyOSX(control);
#elif __ANDROID__ || ANDROID
            if (OperatingSystem.IsAndroid())
                DestroyAndroid(control);
#elif IOS
            if (OperatingSystem.IsIOS())
                DestroyIOS(control);
#endif
            else base.DestroyNativeControlCore(control);
        }
    }
}
