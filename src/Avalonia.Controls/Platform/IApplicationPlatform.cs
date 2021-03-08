using System;

namespace Avalonia.Platform
{
    public interface IApplicationPlatform
    {
        Action<string[]> FilesOpened { get; set; }
    }
}
