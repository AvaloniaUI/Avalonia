using System;
using Avalonia.Native.Interop;
using Avalonia.Platform;

namespace Avalonia.Native
{
    internal class SandboxBookmark : ISandboxBookmark
    {
        private readonly IAvnSandboxBookmark _bookmark;

        public SandboxBookmark(IAvnSandboxBookmark bookmark)
        {
            _bookmark = bookmark;
        }

        public string Url => _bookmark.URL.String;
        public byte[] BookmarkData => _bookmark.Bytes.Bytes;

        public bool DataIsStale => _bookmark.DataIsStale != 0;

        public void Restore() => _bookmark.Restore();

        public IDisposable Open()
        {
            _bookmark.Open();
            return new BookmarkDisposableHandle(_bookmark);
        }

        private class BookmarkDisposableHandle : IDisposable
        {
            private readonly IAvnSandboxBookmark _bookmark;

            public BookmarkDisposableHandle(IAvnSandboxBookmark bookmark)
            {
                _bookmark = bookmark;
            }

            public void Dispose()
            {
                _bookmark.Close();
            }
        }
    }
}
