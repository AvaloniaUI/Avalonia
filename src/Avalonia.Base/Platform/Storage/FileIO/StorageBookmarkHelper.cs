using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

/// <summary>
/// In order to have unique bookmarks across platforms, we prepend a platform specific suffix before native bookmark.
/// And always encoding them in base64 before returning to the user.
/// </summary>
/// <remarks>
/// Bookmarks are encoded as:
/// 0-15  - platform key
/// 16+ - native bookmark value
/// Which is then encoded in Base64.
/// </remarks>
internal static class StorageBookmarkHelper
{
    private const int PrefixLength = 16;

    private static ReadOnlySpan<byte> FakeBclBookmarkPlatform => "avalonia-bcl"u8;

    [return: NotNullIfNotNull(nameof(nativeBookmark))]
    public static string? EncodeBookmark(ReadOnlySpan<byte> platform, string? nativeBookmark) =>
        nativeBookmark is null ? null : EncodeBookmark(platform, Encoding.UTF8.GetBytes(nativeBookmark));

    public static string? EncodeBookmark(ReadOnlySpan<byte> platform, ReadOnlySpan<byte> nativeBookmarkBytes)
    {
        if (nativeBookmarkBytes.Length == 0)
        {
            return null;
        }

        if (platform.Length > PrefixLength)
            throw new ArgumentException($"Platform name should not be longer than {PrefixLength} bytes", nameof(platform));

        var arrayLength = PrefixLength + nativeBookmarkBytes.Length;
        var arrayPool = ArrayPool<byte>.Shared.Rent(arrayLength);
        try
        {
            // Write platform into first 16 bytes.
            var arraySpan = arrayPool.AsSpan(0, arrayLength);
            platform.CopyTo(arraySpan);

            // Write bookmark bytes.
            nativeBookmarkBytes.CopyTo(arraySpan.Slice(PrefixLength));

            // We must use span overload because ArrayPool might return way too big array. 
#if NET6_0_OR_GREATER
            return Convert.ToBase64String(arraySpan);
#else
            return Convert.ToBase64String(arraySpan.ToArray(), Base64FormattingOptions.None);
#endif
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(arrayPool);
        }
    }

    public static bool TryDecodeBookmark(ReadOnlySpan<byte> platform, string? base64bookmark, [NotNullWhen(true)] out byte[]? nativeBookmark)
    {
        if (platform.Length > PrefixLength
            || platform.Length == 0
            || base64bookmark is null
            || base64bookmark.Length % 4 != 0)
        {
            nativeBookmark = null;
            return false;
        }

        Span<byte> decodedBookmark;
#if NET6_0_OR_GREATER
        // Each base64 character represents 6 bits, but to be safe, 
        var arrayPool = ArrayPool<byte>.Shared.Rent(PrefixLength + base64bookmark.Length * 6);
        if (Convert.TryFromBase64Chars(base64bookmark, arrayPool, out int bytesWritten))
        {
            decodedBookmark = arrayPool.AsSpan().Slice(0, bytesWritten);
        }
        else
        {
            nativeBookmark = null;
            return false;
        }
#else
        decodedBookmark = Convert.FromBase64String(base64bookmark).AsSpan();
#endif
        try
        {
            if (decodedBookmark.Length < PrefixLength)
            {
                nativeBookmark = null;
                return false;
            }

            var actualPlatform = decodedBookmark.Slice(0, platform.Length);
            if (actualPlatform.SequenceEqual(platform))
            {
                nativeBookmark = decodedBookmark.Slice(PrefixLength).ToArray();
                return true;
            }

            nativeBookmark = null;
            return false;
        }
        finally
        {
#if NET6_0_OR_GREATER
            ArrayPool<byte>.Shared.Return(arrayPool);
#endif
        }
    }

    public static string EncodeBclBookmark(string localPath) => EncodeBookmark(FakeBclBookmarkPlatform, localPath);

    public static bool TryDecodeBclBookmark(string nativeBookmark, [NotNullWhen(true)] out string? localPath)
    {
        if (TryDecodeBookmark(FakeBclBookmarkPlatform, nativeBookmark, out var bytes))
        {
            localPath = Encoding.UTF8.GetString(bytes);
            return true;
        }

        localPath = null;
        return false;
    }
}
