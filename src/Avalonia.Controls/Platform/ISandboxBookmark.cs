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
        /// Create Mac App Store Sandbox Bookmark from previously saved bookmark data
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
        /// Bookmark Data. Save it to disk as dictionary (string filePath, byte[] bookmarkData).
        /// You will need this to restore a bookmark after app restart using <see cref="ISandboxBookmarkFactory"/>
        /// </summary>
        byte[] BookmarkData { get; }
        
        bool DataIsStale { get; }

        void Restore();

        /// <summary>
        /// Use this method inside a using() every time you want to access a file of that bookmark
        /// </summary>
        IDisposable Open();
    }
}
