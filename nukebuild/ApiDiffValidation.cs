using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common.Tooling;

public static class ApiDiffValidation
{
    private static readonly HttpClient s_httpClient = new();

    private static readonly (string oldTfm, string newTfm)[] s_tfmRedirects = new[]
    {
        // We use StartsWith below comparing these tfm, as we ignore platform versions (like, net6.0-ios16.1)
        ("net6.0-android", "net7.0-android"),
        ("net6.0-ios", "net7.0-ios")
    };

    public static async Task ValidatePackage(
        Tool apiCompatTool, string packagePath, string baselineVersion,
        string suppressionFilesFolder, bool updateSuppressionFile)
    {
        if (baselineVersion is null)
        {
            throw new InvalidOperationException(
                "Build \"api-baseline\" parameter must be set when running Nuke CreatePackages");
        }

        if (!Directory.Exists(suppressionFilesFolder))
        {
            Directory.CreateDirectory(suppressionFilesFolder!);
        }

        await using (var baselineStream = await DownloadBaselinePackage(packagePath, baselineVersion))
        using (var target = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
        using (var baseline = new ZipArchive(baselineStream, ZipArchiveMode.Read))
        using (Helpers.UseTempDir(out var tempFolder))
        {
            var targetDlls = GetDlls(target);
            var baselineDlls = GetDlls(baseline);

            var left = new List<string>();
            var right = new List<string>();

            var packageId = GetPackageId(packagePath);
            var suppressionFile = Path.Combine(suppressionFilesFolder, packageId + ".nupkg.xml");

            // Don't use Path.Combine with these left and right tool parameters.
            // Microsoft.DotNet.ApiCompat.Tool is stupid and treats '/' and '\' as different assemblies in suppression files.
            // So, always use Unix '/'
            foreach (var baselineDll in baselineDlls)
            {
                var baselineDllPath = $"baseline/{baselineDll.target}/{baselineDll.entry.Name}";
                var baselineDllRealPath = Path.Combine(tempFolder, baselineDllPath);
                Directory.CreateDirectory(Path.GetDirectoryName(baselineDllRealPath)!);
                await using (var baselineDllFile = File.Create(baselineDllRealPath))
                {
                    await baselineDll.entry.Open().CopyToAsync(baselineDllFile);
                }

                var targetTfm = baselineDll.target;
                if (s_tfmRedirects.FirstOrDefault(t => baselineDll.target.StartsWith(t.oldTfm)).newTfm is {} newTfm)
                {
                    targetTfm = newTfm;
                }

                var targetDll = targetDlls.FirstOrDefault(e =>
                    e.target.StartsWith(targetTfm) && e.entry.Name == baselineDll.entry.Name);
                if (targetDll.entry is null)
                {
                    throw new InvalidOperationException($"Some assemblies are missing in the new package {packageId}: {baselineDll.entry.Name} for {baselineDll.target}");
                }

                var targetDllPath = $"target/{targetDll.target}/{targetDll.entry.Name}";
                var targetDllRealPath = Path.Combine(tempFolder, targetDllPath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetDllRealPath)!);
                await using (var targetDllFile = File.Create(targetDllRealPath))
                {
                    await targetDll.entry.Open().CopyToAsync(targetDllFile);
                }

                left.Add(baselineDllPath);
                right.Add(targetDllPath);
            }

            if (left.Any())
            {
                var args = $""" -l={string.Join(',', left)} -r="{string.Join(',', right)}" """;
                if (File.Exists(suppressionFile))
                {
                    args += $""" --suppression-file="{suppressionFile}" """;
                }

                if (updateSuppressionFile)
                {
                    args += $""" --suppression-output-file="{suppressionFile}" --generate-suppression-file=true """;
                }

                var result = apiCompatTool(args, tempFolder)
                    .Where(t => t.Type == OutputType.Err).ToArray();
                if (result.Any())
                {
                    throw new AggregateException(
                        $"ApiDiffValidation task has failed for \"{Path.GetFileName(packagePath)}\" package",
                        result.Select(r => new Exception(r.Text)));
                }
            }
        }
    }

    private static IReadOnlyCollection<(string target, ZipArchiveEntry entry)> GetDlls(ZipArchive archive)
    {
        return archive.Entries
            .Where(e => Path.GetExtension(e.FullName) == ".dll"
                // Exclude analyzers and build task, as we don't care about breaking changes there
                && !e.FullName.Contains("analyzers/") && !e.FullName.Contains("analyzers\\")
                && !e.Name.Contains("Avalonia.Build.Tasks"))
            .Select(e => (
                entry: e,
                isRef: e.FullName.Contains("ref/") || e.FullName.Contains("ref\\"),
                target: Path.GetDirectoryName(e.FullName)!.Split(new [] { '/', '\\' }).Last())
            )
            .GroupBy(e => (e.target, e.entry.Name))
            .Select(g => g.MaxBy(e => e.isRef))
            .Select(e => (e.target, e.entry))
            .ToArray();
    }

    static async Task<Stream> DownloadBaselinePackage(string packagePath, string baselineVersion)
    {
        /*
         Gets package name from versions like:
         Avalonia.0.10.0-preview1
         Avalonia.11.0.999-cibuild0037534-beta
         Avalonia.11.0.0
         */
        var packageId = GetPackageId(packagePath);
        Build.Information("Downloading {0} {1} baseline package", packageId, baselineVersion);

        try
        {
            using var response = await s_httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"https://www.nuget.org/api/v2/package/{packageId}/{baselineVersion}"), HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(); 
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Downloading baseline package for {packageId} {baselineVersion} failed.\r" + ex.Message, ex);
        }
    }

    static string GetPackageId(string packagePath)
    {
        return Regex.Replace(
            Path.GetFileNameWithoutExtension(packagePath),
            """(\.\d+\.\d+\.\d+(?:-.+)?)$""", "");
    }
}
