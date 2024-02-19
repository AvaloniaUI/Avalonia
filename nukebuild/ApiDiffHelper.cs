using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nuke.Common.Tooling;
using Serilog;
using static Serilog.Log;

public static class ApiDiffHelper
{
    static readonly HttpClient s_httpClient = new();

    public static async Task GetDiff(
        Tool apiDiffTool, string outputFolder,
        string packagePath, string baselineVersion)
    {
        await using var baselineStream = await DownloadBaselinePackage(packagePath, baselineVersion);
        if (baselineStream == null)
            return;

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder!);
        }

        using (var target = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
        using (var baseline = new ZipArchive(baselineStream, ZipArchiveMode.Read))
        using (Helpers.UseTempDir(out var tempFolder))
        {
            var targetDlls = GetDlls(target);
            var baselineDlls = GetDlls(baseline);

            var pairs = new List<(string baseline, string target)>();

            var packageId = GetPackageId(packagePath);

            // Don't use Path.Combine with these left and right tool parameters.
            // Microsoft.DotNet.ApiCompat.Tool is stupid and treats '/' and '\' as different assemblies in suppression files.
            // So, always use Unix '/'
            foreach (var baselineDll in baselineDlls)
            {
                var baselineDllPath = await ExtractDll("baseline", baselineDll, tempFolder);

                var targetTfm = baselineDll.target;
                var targetDll = targetDlls.FirstOrDefault(e =>
                    e.target.StartsWith(targetTfm) && e.entry.Name == baselineDll.entry.Name);
                if (targetDll is null)
                {
                    if (s_tfmRedirects.FirstOrDefault(t => baselineDll.target.StartsWith(t.oldTfm)).newTfm is {} newTfm)
                    {
                        targetTfm = newTfm;
                        targetDll = targetDlls.FirstOrDefault(e =>
                            e.target.StartsWith(targetTfm) && e.entry.Name == baselineDll.entry.Name);
                    }
                }

                if (targetDll?.entry is null)
                {
                    throw new InvalidOperationException($"Some assemblies are missing in the new package {packageId}: {baselineDll.entry.Name} for {baselineDll.target}");
                }

                var targetDllPath = await ExtractDll("target", targetDll, tempFolder);

                pairs.Add((baselineDllPath, targetDllPath));
            }

            await Task.WhenAll(pairs.Select(p => Task.Run(() =>
            {
                var baselineApi = p.baseline + Random.Shared.Next() + ".api.cs";
                var targetApi = p.target + Random.Shared.Next() + ".api.cs";
                var resultDiff = p.target + ".api.diff.cs";
                
                GenerateApiListing(apiDiffTool, p.baseline, baselineApi, tempFolder);
                GenerateApiListing(apiDiffTool, p.target, targetApi, tempFolder);

                var args = $"""-c core.autocrlf=false diff --no-index --minimal """;
                args += """--ignore-matching-lines="^\[assembly: System.Reflection.AssemblyVersionAttribute" """;
                args += $""" --output {resultDiff} {baselineApi} {targetApi}""";

                using (var gitProcess = new Process())
                {
                    gitProcess.StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = true,
                        RedirectStandardError = false,
                        RedirectStandardOutput = false,
                        FileName = "git",
                        Arguments = args,
                        WorkingDirectory = tempFolder
                    };
                    gitProcess.Start();
                    gitProcess.WaitForExit();
                }

                var resultFile = new FileInfo(Path.Combine(tempFolder, resultDiff));
                if (resultFile.Length > 0)
                {
                    resultFile.CopyTo(Path.Combine(outputFolder, Path.GetFileName(resultDiff)), true);
                }
            })));
        }
    }

    private static readonly (string oldTfm, string newTfm)[] s_tfmRedirects = new[]
    {
        // We use StartsWith below comparing these tfm, as we ignore platform versions (like, net6.0-ios16.1)
        ("net6.0-android", "net7.0-android"),
        ("net6.0-ios", "net7.0-ios"),
        // Designer was moved from netcoreapp to netstandard 
        ("netcoreapp2.0", "netstandard2.0")
    };

    public static async Task ValidatePackage(
        Tool apiCompatTool, string packagePath, string baselineVersion,
        string suppressionFilesFolder, bool updateSuppressionFile)
    {
        if (!Directory.Exists(suppressionFilesFolder))
        {
            Directory.CreateDirectory(suppressionFilesFolder!);
        }

        await using var baselineStream = await DownloadBaselinePackage(packagePath, baselineVersion);
        if (baselineStream == null) 
            return;

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
                var baselineDllPath = await ExtractDll("baseline", baselineDll, tempFolder);

                var targetTfm = baselineDll.target;
                var targetDll = targetDlls.FirstOrDefault(e =>
                    e.target.StartsWith(targetTfm) && e.entry.Name == baselineDll.entry.Name);
                if (targetDll?.entry is null)
                {
                    if (s_tfmRedirects.FirstOrDefault(t => baselineDll.target.StartsWith(t.oldTfm)).newTfm is {} newTfm)
                    {
                        targetTfm = newTfm;
                        targetDll = targetDlls.FirstOrDefault(e =>
                            e.target.StartsWith(targetTfm) && e.entry.Name == baselineDll.entry.Name);
                    }
                }
                if (targetDll?.entry is null && targetDlls.Count == 1)
                {
                    targetDll = targetDlls.First();
                    Warning(
                        $"Some assemblies are missing in the new package {packageId}: {baselineDll.entry.Name} for {baselineDll.target}." +
                        $"Resolved: {targetDll.target} ({targetDll.entry.Name})");
                }

                if (targetDll?.entry is null)
                {
                    var actualTargets = string.Join(", ",
                        targetDlls.Select(d => $"{d.target} ({baselineDll.entry.Name})"));
                    throw new InvalidOperationException(
                        $"Some assemblies are missing in the new package {packageId}: {baselineDll.entry.Name} for {baselineDll.target}."
                        + $"\r\nActual targets: {actualTargets}.");
                }

                var targetDllPath = await ExtractDll("target", targetDll, tempFolder);

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

    record DllEntry(string target, ZipArchiveEntry entry);
    
    static IReadOnlyCollection<DllEntry> GetDlls(ZipArchive archive)
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
            .Select(e => new DllEntry(e.target, e.entry))
            .ToArray();
    }

    static async Task<Stream> DownloadBaselinePackage(string packagePath, string baselineVersion)
    {
        if (baselineVersion is null)
        {
            throw new InvalidOperationException(
                "Build \"api-baseline\" parameter must be set when running Nuke CreatePackages");
        }

        /*
         Gets package name from versions like:
         Avalonia.0.10.0-preview1
         Avalonia.11.0.999-cibuild0037534-beta
         Avalonia.11.0.0
         */
        var packageId = GetPackageId(packagePath);
        Information("Downloading {0} {1} baseline package", packageId, baselineVersion);

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
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Downloading baseline package for {packageId} {baselineVersion} failed.\r" + ex.Message, ex);
        }
    }

    static async Task<string> ExtractDll(string basePath, DllEntry dllEntry, string targetFolder)
    {
        var dllPath = $"{basePath}/{dllEntry.target}/{dllEntry.entry.Name}";
        var dllRealPath = Path.Combine(targetFolder, dllPath);
        Directory.CreateDirectory(Path.GetDirectoryName(dllRealPath)!);
        await using (var dllFile = File.Create(dllRealPath))
        {
            await dllEntry.entry.Open().CopyToAsync(dllFile);
        }

        return dllPath;
    }

    static void GenerateApiListing(Tool apiDiffTool, string inputFile, string outputFile, string workingDif)
    {
        var args = $""" --assembly={inputFile}  --output-path={outputFile}  --include-assembly-attributes=true""";
        var result = apiDiffTool(args, workingDif)
            .Where(t => t.Type == OutputType.Err).ToArray();
        if (result.Any())
        {
            throw new AggregateException($"GetApi tool failed task has failed",
                result.Select(r => new Exception(r.Text)));
        }
    }

    static string GetPackageId(string packagePath)
    {
        return Regex.Replace(
            Path.GetFileNameWithoutExtension(packagePath),
            """(\.\d+\.\d+\.\d+(?:-.+)?)$""", "");
    }
}
