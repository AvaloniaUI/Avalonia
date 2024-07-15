using System;
using System.Linq;
using Avalonia.Platform.Storage.FileIO;
using Xunit;

namespace Avalonia.Controls.UnitTests.Platform;

public class StorageProviderHelperTests
{
    [Fact]
    public void Can_Encode_And_Decode_Bookmark()
    {
        var platform = "test"u8;
        var nativeBookmark = "bookmark"u8;

        var bookmark = StorageBookmarkHelper.EncodeBookmark(platform, nativeBookmark);

        Assert.NotNull(bookmark);

        Assert.True(StorageBookmarkHelper.TryDecodeBookmark(platform, bookmark, out var nativeBookmarkRet));

        Assert.NotNull(nativeBookmarkRet);

        Assert.True(nativeBookmark.SequenceEqual(nativeBookmarkRet));
    }
}
