using Avalonia.Metadata;

namespace Avalonia.Platform
{
    [Unstable("This interface will be removed in 12.0.")]
    public interface IApplicationPlatformEvents
    {
        void RaiseUrlsOpened(string[] urls);
    }
}
