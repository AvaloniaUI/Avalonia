using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable]
    public interface IApplicationPlatformEvents
    {
        void RaiseUrlsOpened(string[] urls);
    }
}
