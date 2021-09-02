namespace Avalonia.Platform
{
    public interface IApplicationPlatformEvents
    {
        void RaiseUrlsOpened(string[] urls);
        void RaiseBookmarkAdded(ISandboxBookmark bookmark);
    }
}
