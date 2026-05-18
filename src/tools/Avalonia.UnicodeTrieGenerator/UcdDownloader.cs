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

        var url = UnicodeDataSource.Ucd + relativePath;
        Console.WriteLine($"  Downloading {url}");

        using var response = s_client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = File.Create(local);
        stream.CopyTo(output);

        return local;
    }
}
