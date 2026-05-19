using System;
using System.IO;
using System.Net.Http;
using Avalonia.Media.TextFormatting.Unicode;

namespace Avalonia.UnicodeTrieGenerator;

internal static class UcdDownloader
{
    private static readonly HttpClient s_client = new();

    public static string CacheRoot { get; set; } =
        Path.Combine(Path.GetTempPath(), "avalonia-ucd-cache");

    // The actual on-disk folder used for downloads. The Unicode version is always
    // appended to CacheRoot so that bumping UnicodeDataSource.Version automatically
    // misses the cache instead of silently reusing the previous version's files.
    public static string VersionedCacheRoot => Path.Combine(CacheRoot, UnicodeDataSource.Version);

    public static Stream OpenRead(string relativePath)
        => File.OpenRead(EnsureDownloaded(relativePath));

    public static string EnsureDownloaded(string relativePath)
    {
        var local = Path.Combine(VersionedCacheRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(local))
        {
            return local;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(local)!);

        // Stale .partial from a previous interrupted run would be picked up as
        // a destination by File.Move below; clear it before starting.
        var tempFile = local + ".partial";
        TryDelete(tempFile);

        var url = UnicodeDataSource.Ucd + relativePath;
        Console.WriteLine($"  Downloading {url}");

        try
        {
            using var response = s_client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            var expectedLength = response.Content.Headers.ContentLength;

            using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
            using (var output = File.Create(tempFile))
            {
                stream.CopyTo(output);
            }

            // If the server advertised Content-Length, make sure we got every byte.
            // Mid-stream truncations don't always raise an exception on the read side.
            if (expectedLength is { } expected)
            {
                var actual = new FileInfo(tempFile).Length;
                if (actual != expected)
                {
                    throw new IOException(
                        $"Download truncated: expected {expected} bytes from {url}, got {actual}.");
                }
            }

            // Promote .partial to its final name only after a clean copy + size check.
            // File.Move is atomic on the same volume, so concurrent readers never see
            // a half-written cache entry.
            File.Move(tempFile, local);
        }
        catch
        {
            // Make sure a failed attempt doesn't masquerade as a cached file next run.
            TryDelete(tempFile);
            throw;
        }

        return local;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; surfacing this would mask the underlying failure.
        }
    }
}
