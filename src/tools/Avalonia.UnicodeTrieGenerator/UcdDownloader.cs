using System;
using System.IO;
using System.Net.Http;

namespace Avalonia.UnicodeTrieGenerator;

internal static class UcdDownloader
{
    public const string UnicodeVersion = "17.0.0";
    public const string Ucd = "https://www.unicode.org/Public/" + UnicodeVersion + "/ucd/";

    private static readonly HttpClient s_client = new();

    public static string CacheRoot { get; set; } =
        Path.Combine(Path.GetTempPath(), "avalonia-ucd-cache", UnicodeVersion);

    public static Stream OpenRead(string relativePath)
        => File.OpenRead(EnsureDownloaded(relativePath));

    public static string EnsureDownloaded(string relativePath)
    {
        var local = Path.Combine(CacheRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(local))
        {
            return local;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(local)!);

        var url = Ucd + relativePath;
        Console.WriteLine($"  Downloading {url}");

        using var response = s_client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = File.Create(local);
        stream.CopyTo(output);

        return local;
    }
}
