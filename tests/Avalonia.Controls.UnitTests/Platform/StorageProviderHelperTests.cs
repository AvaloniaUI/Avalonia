using System;
using System.Linq;
using System.Text;
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
    
    [Theory]
    [InlineData("C://file.txt", "YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==")]
    public void Can_Encode_Bookmark(string nativeBookmark, string expectedEncodedBookmark)
    {
        var platform = "test"u8;

        var bookmark = StorageBookmarkHelper.EncodeBookmark(platform, nativeBookmark);

        Assert.Equal(expectedEncodedBookmark, bookmark);
        Assert.NotNull(bookmark);
    }

    [Theory]
    [InlineData("YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==", "C://file.txt")]
    public void Can_Decode_Bookmark(string encodedBookmark, string expectedNativeBookmark)
    {
        var platform = "test"u8;
        var expectedNativeBookmarkBytes = Encoding.UTF8.GetBytes(expectedNativeBookmark);

        Assert.True(StorageBookmarkHelper.TryDecodeBookmark(platform, encodedBookmark, out var nativeBookmark));

        Assert.Equal(expectedNativeBookmarkBytes, nativeBookmark);
    }
}
