using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Platform
{
    public interface IRuntimePlatform
    {
        Assembly[] GetLoadedAssemblies();
        void PostThreadPoolItem(Action cb);
        IDisposable StartSystemTimer(TimeSpan interval, Action tick);
        string GetStackTrace();
        RuntimePlatformInfo GetRuntimeInfo();
    }

    public struct RuntimePlatformInfo
    {
        public OperatingSystemType OperatingSystem { get; set; }
        public bool IsDesktop { get; set; }
        public bool IsMobile { get; set; }
        public bool IsCoreClr { get; set; }
        public bool IsMono { get; set; }
        public bool IsDotNetFramework { get; set; }
        public bool IsUnix { get; set; }
    }

    public enum OperatingSystemType
    {
        Unknown,
        WinNT,
        Linux,
        OSX,
        Android,
        iOS
    }
}
