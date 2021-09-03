using System;

namespace Avalonia.Platform
{
    public interface ISandboxBookmarkFactory
    {
        ISandboxBookmark Create(byte[] bookmarkData);
    }
    public interface ISandboxBookmark
    {
        string Url { get; }
        byte[] Data { get; }
        IDisposable Open();
    }
}
