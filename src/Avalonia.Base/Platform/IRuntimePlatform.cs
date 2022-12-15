using System;
using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IRuntimePlatform
    {
        IDisposable StartSystemTimer(TimeSpan interval, Action tick);
        RuntimePlatformInfo GetRuntimeInfo();
        IUnmanagedBlob AllocBlob(int size);
    }

    [Unstable]
    public interface IUnmanagedBlob : IDisposable
    {
        IntPtr Address { get; }
        int Size { get; }
        bool IsDisposed { get; }

    }

    [Unstable]
    public record struct RuntimePlatformInfo
    {
        public OperatingSystemType OperatingSystem { get; set; }

        public FormFactorType FormFactor => IsDesktop ? FormFactorType.Desktop :
            IsMobile ? FormFactorType.Mobile : FormFactorType.Unknown;
        public bool IsDesktop { get; set; }
        public bool IsMobile { get; set; }
        public bool IsBrowser { get; set; }
        public bool IsCoreClr { get; set; }
        public bool IsMono { get; set; }
        public bool IsDotNetFramework { get; set; }
        public bool IsUnix { get; set; }
    }

    [Unstable]
    public enum OperatingSystemType
    {
        Unknown,
        WinNT,
        Linux,
        OSX,
        Android,
        iOS,
        Browser
    }

    [Unstable]
    public enum FormFactorType
    {
        Unknown,
        Desktop,
        Mobile
    }
}
