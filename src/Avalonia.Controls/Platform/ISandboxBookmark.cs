using System;

namespace Avalonia.Platform
{
    public interface ISandboxBookmark
    {
        string Url { get; }
        byte[] Data { get; }
        IDisposable Open();
    }
}
