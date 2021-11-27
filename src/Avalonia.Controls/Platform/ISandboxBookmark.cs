using System;

namespace Avalonia.Platform
{
    /// <summary>
    /// Factory to create Mac App Store Sandbox Bookmark from previously saved bookmark data.
    /// Use only in macOS
    /// </summary>
    public interface ISandboxBookmarkFactory
    {
        /// <summary>
        /// Create Mac App Store Sandbox Bookmark from previously saved <see cref="ISandboxBookmark.BookmarkData"/>
        /// </summary>
        ISandboxBookmark Create(byte[] bookmarkData);
    }

    /// <summary>
    /// Mac App Store Sandbox Bookmark - an object that allows to access files outside of sandbox
    /// </summary>
    public interface ISandboxBookmark
    {
        /// <summary>
        /// File Path of the Bookmark
        /// </summary>
        string Url { get; }

        /// <summary>
        /// Bookmark Data. Save it to disk as dictionary of (string filePath, byte[] bookmarkData).
        /// You will need this to restore a bookmark from disk after app restart using <see cref="ISandboxBookmarkFactory"/>
        /// </summary>
        byte[] BookmarkData { get; }
        
        /// <summary>
        /// Bookmark can be stale. If it is - call <see cref="Restore"/> and save new <see cref="BookmarkData"/> to disk.
        /// </summary>
        bool DataIsStale { get; }
        
        /// <summary>
        /// If <see cref="BookmarkData"/> is empty you can look into this field value and find a reason.
        /// </summary>
        string Error { get; }

        /// <summary>
        /// Tries to restore a bookmark if it <see cref="DataIsStale"/>.
        /// Updates <see cref="BookmarkData"/>.
        /// </summary>
        void Restore();

        /// <summary>
        /// Use this method inside a using() every time you want to access a file of that bookmark
        /// After Dispose() - create a new bookmark from a <see cref="ISandboxBookmarkFactory"/>
        /// because app will crash on second open of a single bookmark - osx behavior?. 
        /// </summary>
        IDisposable Open();
    }
}
