#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
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

    private static readonly Regex s_suppressionPathRegex =
        new("<(Left|Right)>(.*?)</(Left|Right)>", RegexOptions.Compiled);

    public static void ValidatePackage(
        Tool apiCompatTool,
        PackageDiffInfo packageDiff,
        AbsolutePath rootAssembliesFolderPath,
        AbsolutePath suppressionFilesFolderPath,
        bool updateSuppressionFile)
    {
        Information("Validating API for package {Id}", packageDiff.PackageId);

        Directory.CreateDirectory(suppressionFilesFolderPath);

        var suppressionFilePath = suppressionFilesFolderPath / (packageDiff.PackageId + ".nupkg.xml");
        var replaceDirectorySeparators = Path.DirectorySeparatorChar == '\\';
        var allErrors = new List<string>();

        foreach (var framework in packageDiff.Frameworks)
        {
            var relativeBaselinePath = rootAssembliesFolderPath.GetRelativePathTo(framework.BaselineFolderPath);
            var relativeCurrentPath = rootAssembliesFolderPath.GetRelativePathTo(framework.CurrentFolderPath);
            var args = "";

            if (suppressionFilePath.FileExists())
            {
                args += $""" --suppression-file="{suppressionFilePath}" --permit-unnecessary-suppressions """;

                if (replaceDirectorySeparators)
                    ReplaceDirectorySeparators(suppressionFilePath, '/', '\\');
            }

            if (updateSuppressionFile)
                args += $""" --suppression-output-file="{suppressionFilePath}" --generate-suppression-file --preserve-unnecessary-suppressions """;

            args += $""" -l="{relativeBaselinePath}" -r="{relativeCurrentPath}" """;

            var localErrors = GetErrors(apiCompatTool($"{args:nq}", rootAssembliesFolderPath, exitHandler: _ => { }));

            if (replaceDirectorySeparators)
                ReplaceDirectorySeparators(suppressionFilePath, '\\', '/');

            allErrors.AddRange(localErrors);
        }

        ThrowOnErrors(allErrors, packageDiff.PackageId, "ValidateApiDiff");
    }

    /// <summary>
    /// The ApiCompat tool treats paths with '/' and '\' separators as different files.
    /// Before running the tool, adjust the existing separators (using a dirty regex) to match the current platform.
    /// After running the tool, change all separators back to '/'.
    /// </summary>
    static void ReplaceDirectorySeparators(AbsolutePath suppressionFilePath, char oldSeparator, char newSeparator)
    {
        if (!File.Exists(suppressionFilePath))
            return;

        var lines = File.ReadAllLines(suppressionFilePath);

        for (var i = 0; i < lines.Length; i++)
        {
            var original = lines[i];

            var replacement = s_suppressionPathRegex.Replace(original, match =>
            {
                var path = match.Groups[2].Value.Replace(oldSeparator, newSeparator);
                return $"<{match.Groups[1].Value}>{path}</{match.Groups[3].Value}>";
            });

            lines[i] = replacement;
        }

        File.WriteAllLines(suppressionFilePath, lines);
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
                packageDiff.Frameworks,
                framework =>
                {
                    var frameworkOutputFolderPath = packageOutputFolderPath / framework.Framework.GetShortFolderName();
                    var args = $""" -b="{framework.BaselineFolderPath}" -bfn="{baselineDisplay}" -a="{framework.CurrentFolderPath}" -afn="{currentDisplay}" -o="{frameworkOutputFolderPath}" -eattrs="{excludedAttributesFilePath}" """;

                    var localErrors = GetErrors(apiDiffTool($"{args:nq}"));

                    if (localErrors.Length > 0)
                    {
                        lock (allErrors)
                            allErrors.AddRange(localErrors);
                    }
                });

            ThrowOnErrors(allErrors, packageDiff.PackageId, "OutputApiDiff");

            MergeFrameworkMarkdownDiffFiles(
                rootOutputFolderPath,
                packageOutputFolderPath,
                [..packageDiff.Frameworks.Select(info => info.Framework)]);

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
        ImmutableArray<NuGetFramework> frameworks)
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

        var assemblyGroups = frameworks
            .SelectMany(GetFrameworkDiffFiles, (framework, filePath) => (framework, filePath))
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
                writer.WriteLine(" (" + string.Join(", ", similarDiffGroup.Select(x => x.framework.GetShortFolderName())) + ")");

                while (reader.ReadLine() is { } line)
                    writer.WriteLine(line);

                addSeparator = true;
            }
        }

        AbsolutePath[] GetFrameworkDiffFiles(NuGetFramework framework)
        {
            var frameworkFolderPath = packageOutputFolderPath / framework.GetShortFolderName();
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
        const string mergedFileName = "_diff.md";

        var filePaths = Directory.EnumerateFiles(rootOutputFolderPath, "*.md")
            .Where(filePath => Path.GetFileName(filePath) != mergedFileName)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        using var writer = File.CreateText(rootOutputFolderPath / mergedFileName);

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
            Dictionary<NuGetFramework, string> currentFolderNames;
            Dictionary<NuGetFramework, string> baselineFolderNames;

            // Extract current package
            using (var currentArchive = new ZipArchive(File.OpenRead(packagePath), ZipArchiveMode.Read, leaveOpen: false))
            {
                using var packageReader = new PackageArchiveReader(currentArchive);
                packageId = packageReader.NuspecReader.GetId();
                currentFolderPath = outputFolderPath / "current" / packageId;
                currentFolderNames = ExtractDiffableAssembliesFromPackage(currentArchive, currentFolderPath);
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
                baselineFolderNames = ExtractDiffableAssembliesFromPackage(baselineArchive, baselineFolderPath);
            }

            if (currentFolderNames.Count == 0 && baselineFolderNames.Count == 0)
                continue;

            var frameworkDiffs = new List<FrameworkDiffInfo>();

            foreach (var (framework, currentFolderName) in currentFolderNames)
            {
                // Ignore new frameworks that didn't exist in the baseline package. Empty folders make the ApiDiff tool crash.
                if (!baselineFolderNames.TryGetValue(framework, out var baselineFolderName))
                    continue;

                frameworkDiffs.Add(new FrameworkDiffInfo(
                    framework,
                    baselineFolderPath / FolderLib / baselineFolderName,
                    currentFolderPath / FolderLib / currentFolderName));
            }

            packageDiffs.Add(new PackageDiffInfo(packageId, [..frameworkDiffs]));
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

    static Dictionary<NuGetFramework, string> ExtractDiffableAssembliesFromPackage(
        ZipArchive packageArchive,
        AbsolutePath destinationFolderPath)
    {
        var folderByFramework = new Dictionary<NuGetFramework, string>();

        foreach (var entry in packageArchive.Entries)
        {
            if (TryGetFrameworkFolderName(entry.FullName) is not { } folderName)
                continue;

            // Ignore platform versions: assume that e.g. net8.0-android34 and net8.0-android35 are the same for diff purposes.
            var framework = WithoutPlatformVersion(NuGetFramework.ParseFolder(folderName));

            if (folderByFramework.TryGetValue(framework, out var existingFolderName))
            {
                if (existingFolderName != folderName)
                {
                    throw new InvalidOperationException(
                        $"Found two similar frameworks with different platform versions: {existingFolderName} and {folderName}");
                }
            }
            else
                folderByFramework.Add(framework, folderName);

            var targetFilePath = destinationFolderPath / entry.FullName;
            Directory.CreateDirectory(targetFilePath.Parent);
            entry.ExtractToFile(targetFilePath, overwrite: true);
        }

        return folderByFramework;

        static string? TryGetFrameworkFolderName(string entryPath)
        {
            if (!entryPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                return null;

            var segments = entryPath.Split('/');
            if (segments is not [FolderLib, var name, ..])
                return null;

            return name;
        }

        // e.g. net8.0-android34.0 to net8.0-android
        static NuGetFramework WithoutPlatformVersion(NuGetFramework value)
            => value.HasPlatform && value.PlatformVersion != FrameworkConstants.EmptyVersion ?
                new NuGetFramework(value.Framework, value.Version, value.Platform, FrameworkConstants.EmptyVersion) :
                value;
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

    public sealed class PackageDiffInfo(string packageId, ImmutableArray<FrameworkDiffInfo> frameworks)
    {
        public string PackageId { get; } = packageId;
        public ImmutableArray<FrameworkDiffInfo> Frameworks { get; } = frameworks;
    }

    public sealed class FrameworkDiffInfo(
        NuGetFramework framework,
        AbsolutePath baselineFolderPath,
        AbsolutePath currentFolderPath)
    {
        public NuGetFramework Framework { get; } = framework;
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
