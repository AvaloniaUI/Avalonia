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
        public FormFactorType FormFactor => IsDesktop ? FormFactorType.Desktop :
            IsMobile ? FormFactorType.Mobile : FormFactorType.Unknown;
        public bool IsDesktop { get; set; }
        public bool IsMobile { get; set; }
    }

    [Unstable]
    public enum FormFactorType
    {
        Unknown,
        Desktop,
        Mobile
    }
}
