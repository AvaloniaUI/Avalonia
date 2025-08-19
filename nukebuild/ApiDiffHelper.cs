#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Serilog.Log;

public static class ApiDiffHelper
{
    const string NightlyFeedUri = "https://nuget-feed-nightly.avaloniaui.net/v3/index.json";
    const string MainPackageName = "Avalonia";
    const string FolderLib = "lib";

    public static void ValidatePackage(
        Tool apiCompatTool,
        PackageDiffInfo packageDiff,
        AbsolutePath suppressionFilesFolderPath,
        bool updateSuppressionFile)
    {
        Information("Validating API for package {Id}", packageDiff.PackageId);

        Directory.CreateDirectory(suppressionFilesFolderPath);

        var suppressionArgs = "";

        var suppressionFile = suppressionFilesFolderPath / (packageDiff.PackageId + ".nupkg.xml");
        if (suppressionFile.FileExists())
            suppressionArgs += $""" --suppression-file="{suppressionFile}" """;

        if (updateSuppressionFile)
            suppressionArgs += $""" --suppression-output-file="{suppressionFile}" --generate-suppression-file=true """;

        var allErrors = new List<string>();

        Parallel.ForEach(
            packageDiff.TargetFrameworks,
            targetFramework =>
            {
                var baselineFrameworkPath = packageDiff.BaselineFolderPath / FolderLib / targetFramework;
                var currentFrameworkPath = packageDiff.CurrentFolderPath / FolderLib / targetFramework;
                var args = $""" -l="{baselineFrameworkPath}" -r="{currentFrameworkPath}" {suppressionArgs}""";

                var localErrors = GetErrors(apiCompatTool(args));

                if (localErrors.Length > 0)
                {
                    lock (allErrors)
                        allErrors.AddRange(localErrors);
                }
            });

        ThrowOnErrors(allErrors, packageDiff.PackageId, "ValidateApiDiff");
    }

    public static void GenerateMarkdownDiff(
        Tool apiDiffTool,
        PackageDiffInfo packageDiff,
        AbsolutePath rootOutputFolderPath,
        string baselineDisplay,
        string currentDisplay)
    {
        Information("Creating markdown diff for package {Id}", packageDiff.PackageId);

        var packageOutputFolderPath = rootOutputFolderPath / packageDiff.PackageId;
        Directory.CreateDirectory(packageOutputFolderPath);

        // Not specifying -eattrs incorrectly tries to load AttributesToExclude.txt, create an empty file instead.
        // See https://github.com/dotnet/sdk/issues/49719
        var excludedAttributesFilePath = (AbsolutePath)Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        File.WriteAllBytes(excludedAttributesFilePath!, []);

        try
        {
            var allErrors = new List<string>();

            // The API diff tool is unbelievably slow, process in parallel.
            Parallel.ForEach(
                packageDiff.TargetFrameworks,
                targetFramework =>
                {
                    var baselineFrameworkPath = packageDiff.BaselineFolderPath / FolderLib / targetFramework;
                    var currentFrameworkPath = packageDiff.CurrentFolderPath / FolderLib / targetFramework;
                    var outputFrameworkPath = packageOutputFolderPath / targetFramework;
                    var args = $""" -b="{baselineFrameworkPath}" -bfn="{baselineDisplay}" -a="{currentFrameworkPath}" -afn="{currentDisplay}" -o="{outputFrameworkPath}" -eattrs="{excludedAttributesFilePath}" """;

                    var localErrors = GetErrors(apiDiffTool(args));

                    if (localErrors.Length > 0)
                    {
                        lock (allErrors)
                            allErrors.AddRange(localErrors);
                    }
                });

            ThrowOnErrors(allErrors, packageDiff.PackageId, "OutputApiDiff");

            MergeFrameworkMarkdownDiffFiles(rootOutputFolderPath, packageOutputFolderPath, packageDiff.TargetFrameworks);
            Directory.Delete(packageOutputFolderPath, true);
        }
        finally
        {
            File.Delete(excludedAttributesFilePath);
        }
    }

    static void MergeFrameworkMarkdownDiffFiles(
        AbsolutePath rootOutputFolderPath,
        AbsolutePath packageOutputFolderPath,
        ImmutableArray<string> targetFrameworks)
    {
        // At this point, the hierarchy looks like:
        //   markdown/
        //   ├─ net8.0/
        //   │  ├─ api_diff_Avalonia.md
        //   │  ├─ api_diff_Avalonia.Controls.md
        //   ├─ netstandard2.0/
        //   │  ├─ api_diff_Avalonia.md
        //   │  ├─ api_diff_Avalonia.Controls.md
        //
        // We want one file per assembly: merge all files with the same name.
        // However, it's very likely that the diff is the same for several frameworks: in this case, keep only one file.

        var assemblyGroups = targetFrameworks
            .SelectMany(GetFrameworkDiffFiles, (targetFramework, filePath) => (targetFramework, filePath))
            .GroupBy(x => x.filePath.Name)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyGroup in assemblyGroups)
        {
            using var writer = File.CreateText(rootOutputFolderPath / assemblyGroup.Key.Replace("api_diff_", ""));
            var addSeparator = false;

            foreach (var similarDiffGroup in assemblyGroup.GroupBy(x => HashFile(x.filePath), ByteArrayEqualityComparer.Instance))
            {
                if (addSeparator)
                    writer.WriteLine();

                using var reader = File.OpenText(similarDiffGroup.First().filePath);
                var firstLine = reader.ReadLine();

                writer.Write(firstLine);
                writer.WriteLine(" (" + string.Join(", ", similarDiffGroup.Select(x => x.targetFramework)) + ")");

                while (reader.ReadLine() is { } line)
                    writer.WriteLine(line);

                addSeparator = true;
            }
        }

        AbsolutePath[] GetFrameworkDiffFiles(string targetFramework)
        {
            var frameworkFolderPath = packageOutputFolderPath / targetFramework;
            if (!frameworkFolderPath.DirectoryExists())
                return [];

            return Directory.GetFiles(frameworkFolderPath, "*.md")
                .Where(filePath => Path.GetFileName(filePath) != "api_diff.md")
                .Select(filePath => (AbsolutePath)filePath)
                .ToArray();
        }

        static byte[] HashFile(AbsolutePath filePath)
        {
            using var stream = File.OpenRead(filePath);
            return SHA256.HashData(stream);
        }
    }

    public static void MergePackageMarkdownDiffFiles(
        AbsolutePath rootOutputFolderPath,
        string baselineDisplay,
        string currentDisplay)
    {
        var filePaths = Directory.GetFiles(rootOutputFolderPath, "*.md");
        Array.Sort(filePaths, StringComparer.OrdinalIgnoreCase);

        using var writer = File.CreateText(rootOutputFolderPath / "_diff.md");

        writer.WriteLine($"# API diff between {baselineDisplay} and {currentDisplay}");

        if (filePaths.Length == 0)
        {
            writer.WriteLine();
            writer.WriteLine("No changes.");
            return;
        }

        foreach (var filePath in filePaths)
        {
            writer.WriteLine();

            using var reader = File.OpenText(filePath);

            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith('#'))
                    writer.Write('#');

                writer.WriteLine(line);
            }
        }
    }

    static string[] GetErrors(IEnumerable<Output> outputs)
        => outputs
            .Where(output => output.Type == OutputType.Err)
            .Select(output => output.Text)
            .ToArray();

    static void ThrowOnErrors(List<string> errors, string packageId, string taskName)
    {
        if (errors.Count > 0)
        {
            throw new AggregateException(
                $"{taskName} task has failed for \"{packageId}\" package",
                errors.Select(error => new Exception(error)));
        }
    }

    public static async Task<GlobalDiffInfo> DownloadAndExtractPackagesAsync(
        IEnumerable<AbsolutePath> currentPackagePaths,
        NuGetVersion currentVersion,
        bool isReleaseBranch,
        AbsolutePath outputFolderPath,
        NuGetVersion? forcedBaselineVersion)
    {
        var downloadContext = await CreateNuGetDownloadContextAsync();
        var baselineVersion = forcedBaselineVersion ??
                              await GetBaselineVersionAsync(downloadContext, currentVersion, isReleaseBranch);

        Information("API baseline version is {Baseline} for current version {Current}", baselineVersion, currentVersion);

        var memoryStream = new MemoryStream();
        var packageDiffs = ImmutableArray.CreateBuilder<PackageDiffInfo>();

        foreach (var packagePath in currentPackagePaths)
        {
            string packageId;
            AbsolutePath currentFolderPath;
            AbsolutePath baselineFolderPath;
            var targetFrameworks = new HashSet<string>();

            // Extract current package
            using (var currentArchive = new ZipArchive(File.OpenRead(packagePath), ZipArchiveMode.Read, leaveOpen: false))
            {
                using var packageReader = new PackageArchiveReader(currentArchive);
                packageId = packageReader.NuspecReader.GetId();
                currentFolderPath = outputFolderPath / "current" / packageId;
                ExtractDiffableAssembliesFromPackage(currentArchive, currentFolderPath, targetFrameworks);
            }

            // Download baseline package
            memoryStream.Position = 0L;
            memoryStream.SetLength(0L);
            await DownloadBaselinePackageAsync(memoryStream, downloadContext, packageId, baselineVersion);
            memoryStream.Position = 0L;

            // Extract baseline package
            using (var baselineArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                baselineFolderPath = outputFolderPath / "baseline" / packageId;
                ExtractDiffableAssembliesFromPackage(baselineArchive, baselineFolderPath, targetFrameworks);
            }

            if (targetFrameworks.Count == 0)
                continue;

            // Ensure target framework folders exist on both sides
            foreach (var targetFramework in targetFrameworks)
            {
                Directory.CreateDirectory(baselineFolderPath / FolderLib / targetFramework);
                Directory.CreateDirectory(currentFolderPath / FolderLib / targetFramework);
            }

            packageDiffs.Add(new PackageDiffInfo(packageId, [..targetFrameworks], baselineFolderPath, currentFolderPath));
        }

        return new GlobalDiffInfo(baselineVersion, currentVersion, packageDiffs.DrainToImmutable());
    }

    static async Task<NuGetDownloadContext> CreateNuGetDownloadContextAsync()
    {
        var packageSource = new PackageSource(NightlyFeedUri) { ProtocolVersion = 3 };
        var repository = Repository.Factory.GetCoreV3(packageSource);
        var findPackageByIdResource = await repository.GetResourceAsync<FindPackageByIdResource>();
        return new NuGetDownloadContext(packageSource, findPackageByIdResource);
    }

    /// <summary>
    /// Finds the baseline version to diff against.
    /// On release branches, use the latest stable version.
    /// On the main branch and on PRs, use the latest nightly version.
    /// This method assumes all packages share the same version.
    /// </summary>
    static async Task<NuGetVersion> GetBaselineVersionAsync(
        NuGetDownloadContext context,
        NuGetVersion currentVersion,
        bool isReleaseBranch)
    {
        var versions = await context.FindPackageByIdResource.GetAllVersionsAsync(
            MainPackageName,
            context.CacheContext,
            NullLogger.Instance,
            CancellationToken.None);

        versions = versions.Where(v => v < currentVersion);

        if (isReleaseBranch)
            versions = versions.Where(v => !v.IsPrerelease);

        return versions.OrderDescending().FirstOrDefault()
           ?? throw new InvalidOperationException(
               $"Could not find a version less than {currentVersion} for package {MainPackageName} in source {context.PackageSource.Source}");
    }

    static async Task DownloadBaselinePackageAsync(
        Stream destinationStream,
        NuGetDownloadContext context,
        string packageId,
        NuGetVersion version)
    {
        Information("Downloading {Id} {Version} baseline package", packageId, version);

        var downloaded = await context.FindPackageByIdResource.CopyNupkgToStreamAsync(
            packageId,
            version,
            destinationStream,
            context.CacheContext,
            NullLogger.Instance,
            CancellationToken.None);

        if (!downloaded)
        {
            throw new InvalidOperationException(
                $"Could not download version {version} for package {packageId} in source {context.PackageSource.Source}");
        }
    }

    static void ExtractDiffableAssembliesFromPackage(
        ZipArchive packageArchive,
        AbsolutePath destinationFolderPath,
        HashSet<string> outputTargetFrameworks)
    {
        foreach (var entry in packageArchive.Entries)
        {
            if (GetTargetFramework(entry) is not { } targetFramework)
                continue;

            outputTargetFrameworks.Add(targetFramework);
            var targetFilePath = destinationFolderPath / entry.FullName;
            Directory.CreateDirectory(targetFilePath.Parent);
            entry.ExtractToFile(targetFilePath, overwrite: true);
        }

        static string? GetTargetFramework(ZipArchiveEntry entry)
        {
            if (!entry.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return null;

            var segments = entry.FullName.Split('/');
            if (segments is not [FolderLib, var targetFramework, ..])
                return null;

            return targetFramework;
        }
    }

    public sealed class GlobalDiffInfo(
        NuGetVersion baselineVersion,
        NuGetVersion currentVersion,
        ImmutableArray<PackageDiffInfo> packages)
    {
        public NuGetVersion BaselineVersion { get; } = baselineVersion;
        public NuGetVersion CurrentVersion { get; } = currentVersion;
        public ImmutableArray<PackageDiffInfo> Packages { get; } = packages;
    }

    public sealed class PackageDiffInfo(
        string packageId,
        ImmutableArray<string> targetFrameworks,
        AbsolutePath baselineFolderPath,
        AbsolutePath currentFolderPath)
    {
        public string PackageId { get; } = packageId;
        public ImmutableArray<string> TargetFrameworks { get; } = targetFrameworks;
        public AbsolutePath BaselineFolderPath { get; } = baselineFolderPath;
        public AbsolutePath CurrentFolderPath { get; } = currentFolderPath;
    }

    sealed class NuGetDownloadContext(PackageSource packageSource, FindPackageByIdResource findPackageByIdResource)
    {
        public SourceCacheContext CacheContext { get; } = new();
        public PackageSource PackageSource { get; } = packageSource;
        public FindPackageByIdResource FindPackageByIdResource { get; } = findPackageByIdResource;
    }
}
