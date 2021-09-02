using System;
using Avalonia.Platform;

namespace Avalonia
{
    public class SandboxBookmarkAddedEventArgs : EventArgs
    {
        public SandboxBookmarkAddedEventArgs(ISandboxBookmark bookmark)
        {
            Bookmark = bookmark;
        }
        
        public ISandboxBookmark Bookmark { get; }
    }
}
