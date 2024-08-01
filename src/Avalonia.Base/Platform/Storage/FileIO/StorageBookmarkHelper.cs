using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace Avalonia.Platform.Storage.FileIO;

/// <summary>
/// In order to have unique bookmarks across platforms, we prepend a platform specific suffix before native bookmark.
/// And always encoding them in base64 before returning to the user.
/// </summary>
/// <remarks>
/// Bookmarks are encoded as:
/// 0-6 - avalonia prefix with version number
/// 7-15  - platform key
/// 16+ - native bookmark value
/// Which is then encoded in Base64.
/// </remarks>
internal static class StorageBookmarkHelper
{
    private const int HeaderLength = 16;
    private static ReadOnlySpan<byte> AvaHeaderPrefix => "ava.v1."u8;
    private static ReadOnlySpan<byte> FakeBclBookmarkPlatform => "bcl"u8;

    [return: NotNullIfNotNull(nameof(nativeBookmark))]
    public static string? EncodeBookmark(ReadOnlySpan<byte> platform, string? nativeBookmark) =>
        nativeBookmark is null ? null : EncodeBookmark(platform, Encoding.UTF8.GetBytes(nativeBookmark));

    public static string? EncodeBookmark(ReadOnlySpan<byte> platform, ReadOnlySpan<byte> nativeBookmarkBytes)
    {
        if (nativeBookmarkBytes.Length == 0)
        {
            return null;
        }

        if (platform.Length > HeaderLength)
        {
            throw new ArgumentException($"Platform name should not be longer than {HeaderLength} bytes", nameof(platform));
        }

        var arrayLength = HeaderLength + nativeBookmarkBytes.Length;
        var arrayPool = ArrayPool<byte>.Shared.Rent(arrayLength);
        try
        {
            // Write platform into first 16 bytes.
            var arraySpan = arrayPool.AsSpan(0, arrayLength);
            AvaHeaderPrefix.CopyTo(arraySpan);
            platform.CopyTo(arraySpan.Slice(AvaHeaderPrefix.Length));

            // Write bookmark bytes.
            nativeBookmarkBytes.CopyTo(arraySpan.Slice(HeaderLength));

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

    public enum DecodeResult
    {
        Success = 0,
        InvalidFormat,
        InvalidPlatform
    }

    public static DecodeResult TryDecodeBookmark(ReadOnlySpan<byte> platform, string? base64bookmark, out byte[]? nativeBookmark)
    {
        if (platform.Length > HeaderLength
            || platform.Length == 0
            || base64bookmark is null
            || base64bookmark.Length % 4 != 0)
        {
            nativeBookmark = null;
            return DecodeResult.InvalidFormat;
        }

        Span<byte> decodedBookmark;
#if NET6_0_OR_GREATER
        // Each base64 character represents 6 bits, but to be safe, 
        var arrayPool = ArrayPool<byte>.Shared.Rent(HeaderLength + base64bookmark.Length * 6);
        if (Convert.TryFromBase64Chars(base64bookmark, arrayPool, out int bytesWritten))
        {
            decodedBookmark = arrayPool.AsSpan().Slice(0, bytesWritten);
        }
        else
        {
            nativeBookmark = null;
            return DecodeResult.InvalidFormat;
        }
#else
        decodedBookmark = Convert.FromBase64String(base64bookmark).AsSpan();
#endif
        try
        {
            if (decodedBookmark.Length < HeaderLength
                // Check if decoded string starts with the correct prefix, checking v1 at the same time.
                && !AvaHeaderPrefix.SequenceEqual(decodedBookmark.Slice(0, AvaHeaderPrefix.Length)))
            {
                nativeBookmark = null;
                return DecodeResult.InvalidFormat;
            }

            var actualPlatform = decodedBookmark.Slice(AvaHeaderPrefix.Length, platform.Length);
            if (!actualPlatform.SequenceEqual(platform))
            {
                nativeBookmark = null;
                return DecodeResult.InvalidPlatform;
            }

            nativeBookmark = decodedBookmark.Slice(HeaderLength).ToArray();
            return DecodeResult.Success;
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
        var decodeResult = TryDecodeBookmark(FakeBclBookmarkPlatform, nativeBookmark, out var bytes);
        if (decodeResult == DecodeResult.Success)
        {
            localPath = Encoding.UTF8.GetString(bytes!);
            return true;
        }
        if (decodeResult == DecodeResult.InvalidFormat
            && nativeBookmark.IndexOfAny(Path.GetInvalidPathChars()) < 0
            && !string.IsNullOrEmpty(Path.GetDirectoryName(nativeBookmark)))
        {
            // Attempt to restore old BCL bookmarks.
            // Don't check for File.Exists here, as it will be done at later point in TryGetStorageItem.
            // Just validate if it looks like a valid file path.
            localPath = nativeBookmark;
            return true;
        }

        localPath = null;
        return false;
    }
}
