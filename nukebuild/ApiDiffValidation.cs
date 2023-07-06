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
    public static async Task ValidatePackage(
        Tool apiCompatTool, string packagePath, Version baselineVersion,
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

        using (var baselineStream = await DownloadBaselinePackage(packagePath, baselineVersion))
        using (var target = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
        using (var baseline = new ZipArchive(baselineStream, ZipArchiveMode.Read))
        using (Helpers.UseTempDir(out var tempFolder))
        {
            var targetDlls = GetDlls(target);
            var baselineDlls = GetDlls(baseline);

            var left = new List<string>();
            var right = new List<string>();

            var suppressionFile = Path.Combine(suppressionFilesFolder, Path.GetFileName(packagePath) + ".xml");

            foreach (var baselineDll in baselineDlls)
            {
                var baselineDllPath = Path.Combine("baseline", baselineDll.target, baselineDll.entry.Name);
                var baselineDllRealPath = Path.Combine(tempFolder, baselineDllPath);
                Directory.CreateDirectory(Path.GetDirectoryName(baselineDllRealPath)!);
                using (var baselineDllFile = File.Create(baselineDllRealPath))
                {
                    await baselineDll.entry.Open().CopyToAsync(baselineDllFile);
                }

                var targetDll = targetDlls.FirstOrDefault(e =>
                    e.target == baselineDll.target && e.entry.Name == baselineDll.entry.Name);
                if (targetDll.entry is null)
                {
                    throw new InvalidOperationException($"Some assemblies are missing in the new package: {baselineDll.entry.Name} for {baselineDll.target}");
                }

                var targetDllPath = Path.Combine("target", targetDll.target, targetDll.entry.Name);
                var targetDllRealPath = Path.Combine(tempFolder, targetDllPath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetDllRealPath)!);
                using (var targetDllFile = File.Create(targetDllRealPath))
                {
                    await targetDll.entry.Open().CopyToAsync(targetDllFile);
                }

                left.Add(baselineDllPath);
                right.Add(targetDllPath);
            }

            var args = $""" -l={string.Join(',', left)} -r="{string.Join(',', right)}" """;
            if (File.Exists(suppressionFile))
            {
                args += $""" --suppression-file="{suppressionFile}" """;
            }
            if (updateSuppressionFile)
            {
                args += $""" --suppression-output-file="{suppressionFile}" --generate-suppression-file=true """;
            }

            apiCompatTool(args, tempFolder);
        }
    }

    private static IReadOnlyCollection<(string target, ZipArchiveEntry entry)> GetDlls(ZipArchive archive)
    {
        return archive.Entries
            .Where(e => Path.GetExtension(e.FullName) == ".dll")
            .Select(e => (
                entry: e,
                isRef: e.FullName.Contains("ref/"),
                target: Path.GetDirectoryName(e.FullName)!.Split('/').Last())
            )
            .GroupBy(e => (e.target, e.entry.Name))
            .Select(g => g.MaxBy(e => e.isRef))
            .Select(e => (e.target, e.entry))
            .ToArray();
    }

    static async Task<Stream> DownloadBaselinePackage(string packagePath, Version baselineVersion)
    {
        Build.Information("Downloading {0} baseline package for version {1}", Path.GetFileName(packagePath), baselineVersion);

        try
        {
            var packageId = Regex.Replace(
                Path.GetFileNameWithoutExtension(packagePath),
                """(\.\d+\.\d+\.\d+)$""", "");

            using var httpClient = new HttpClient();
            using var response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"https://www.nuget.org/api/v2/package/{packageId}/{baselineVersion}"));
            await using var stream = await response.Content.ReadAsStreamAsync(); 
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Downloading baseline package for {packagePath} failed.\r" + ex.Message, ex);
        }
    }
}
