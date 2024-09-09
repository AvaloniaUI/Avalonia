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

        Assert.Equal(
            StorageBookmarkHelper.DecodeResult.Success,
            StorageBookmarkHelper.TryDecodeBookmark(platform, bookmark, out var nativeBookmarkRet));

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

        Assert.Equal(
            StorageBookmarkHelper.DecodeResult.Success,
            StorageBookmarkHelper.TryDecodeBookmark(platform, encodedBookmark, out var nativeBookmark));

        Assert.Equal(expectedNativeBookmarkBytes, nativeBookmark);
    }

    [Theory]
    [InlineData("YXZhLnYxLmJjbAAAAAAAAEM6Ly9maWxlLnR4dA==", "C://file.txt")]
    [InlineData("C://file.txt", "C://file.txt")]
    public void Can_Decode_Bcl_Bookmarks(string bookmark, string expected)
    {
        var a = StorageBookmarkHelper.EncodeBclBookmark(expected);
        Assert.True(StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var localPath));
        Assert.Equal(expected, localPath);
    }

    [Theory]
    [InlineData("YXZhLnYxLnRlc3QAAAAAAEM6Ly9maWxlLnR4dA==")] // "test" platform passed instead of "bcl"
    [InlineData("ZYXasHKJASd87124")]
    public void Fails_To_Decode_Invalid_Bcl_Bookmarks(string bookmark)
    { 
        Assert.False(StorageBookmarkHelper.TryDecodeBclBookmark(bookmark, out var localPath));
        Assert.Null(localPath);
    }
}
