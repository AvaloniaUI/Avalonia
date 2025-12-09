#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.IO;

public partial class Build
{
    [Parameter(Name = "configuration")]
    public string? Configuration { get; set; }

    [Parameter(Name = "skip-tests")]
    public bool SkipTests { get; set; }

    [Parameter(Name = "force-nuget-version")]
    public string? ForceNugetVersion { get; set; }

    [Parameter(Name = "force-api-baseline")]
    public string? ForceApiValidationBaseline { get; set; }

    [Parameter(Name = "update-api-suppression")]
    public bool? UpdateApiValidationSuppression { get; set; }

    [Parameter(Name = "version-output-dir")]
    public AbsolutePath? VersionOutputDir { get; set; }

    public class BuildParameters
    {
        public string Configuration { get; }
        public bool SkipTests { get; }
        public string MainRepo { get; }
        public string MasterBranch { get; }
        public string? RepositoryName { get; }
        public string? RepositoryBranch { get; }
        public string ReleaseConfiguration { get; }
        public Regex ReleaseBranchRegex { get; }
        public string MSBuildSolution { get; }
        public bool IsLocalBuild { get; }
        public bool IsRunningOnUnix { get; }
        public bool IsRunningOnWindows { get; }
        public bool IsRunningOnAzure { get; }
        public bool IsPullRequest { get; }
        public bool IsMainRepo { get; }
        public bool IsMasterBranch { get; }
        public bool IsReleaseBranch { get; }
        public bool IsReleasable { get; }
        public bool IsMyGetRelease { get; }
        public bool IsNuGetRelease { get; }
        public bool PublishTestResults { get; }
        public string Version { get; set; }
        public const string LocalBuildVersion = "9999.0.0-localbuild";
        public bool IsPackingToLocalCache { get; private set; }

        public AbsolutePath ArtifactsDir { get; }
        public AbsolutePath NugetIntermediateRoot { get; }
        public AbsolutePath NugetRoot { get; }
        public AbsolutePath ZipRoot { get; }
        public AbsolutePath TestResultsRoot { get; }
        public string DirSuffix { get; }
        public List<AbsolutePath> BuildDirs { get; }
        public string FileZipSuffix { get; }
        public AbsolutePath ZipCoreArtifacts { get; }
        public AbsolutePath ZipNuGetArtifacts { get; }
        public string? ForceApiValidationBaseline { get; }
        public bool UpdateApiValidationSuppression { get; }
        public AbsolutePath ApiValidationSuppressionFiles { get; }
        public AbsolutePath? VersionOutputDir { get; }

        public BuildParameters(Build b, bool isPackingToLocalCache)
        {
            // ARGUMENTS
            Configuration = b.Configuration ?? "Release";
            SkipTests = b.SkipTests;

            // CONFIGURATION
            MainRepo = "https://github.com/AvaloniaUI/Avalonia";
            MasterBranch = "refs/heads/master";
            ReleaseBranchRegex = new("^refs/heads/release/(0|[1-9]\\d*)\\.(0|[1-9]\\d*)\\.(0|[1-9]\\d*)(?:-((?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\\.(?:0|[1-9]\\d*|\\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\\+([0-9a-zA-Z-]+(?:\\.[0-9a-zA-Z-]+)*))?$");
            ReleaseConfiguration = "Release";
            MSBuildSolution = RootDirectory / "dirs.proj";

            // PARAMETERS
            IsLocalBuild = NukeBuild.IsLocalBuild;
            IsRunningOnUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                              Environment.OSVersion.Platform == PlatformID.MacOSX;
            IsRunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsRunningOnAzure = Host is AzurePipelines;

            if (IsRunningOnAzure)
            {
                RepositoryName = AzurePipelines.Instance.RepositoryUri;
                RepositoryBranch = AzurePipelines.Instance.SourceBranch;
                IsPullRequest = AzurePipelines.Instance.PullRequestId.HasValue;
                IsMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, AzurePipelines.Instance.RepositoryUri);
            }
            IsMainRepo =
                StringComparer.OrdinalIgnoreCase.Equals(MainRepo,
                    RepositoryName);
            IsMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch,
                RepositoryBranch);
            IsReleaseBranch = RepositoryBranch is not null && ReleaseBranchRegex.IsMatch(RepositoryBranch);

            IsReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, Configuration);
            IsMyGetRelease = IsReleasable;
            IsNuGetRelease = IsMainRepo && IsReleasable && IsReleaseBranch;

            // VERSION
            Version = b.ForceNugetVersion ?? GetVersion();

            ForceApiValidationBaseline = b.ForceApiValidationBaseline;
            UpdateApiValidationSuppression = b.UpdateApiValidationSuppression ?? IsLocalBuild;
            
            if (IsRunningOnAzure)
            {
                if (!IsNuGetRelease)
                {
                    // Use AssemblyVersion with Build as version
                    var buildId = Environment.GetEnvironmentVariable("BUILD_BUILDID") ??
                                  throw new InvalidOperationException("Missing environment variable BUILD_BUILDID");
                    Version += "-cibuild" + int.Parse(buildId).ToString("0000000") + "-alpha";
                }

                PublishTestResults = true;
            }
            
            if (isPackingToLocalCache)
            {
                IsPackingToLocalCache = true;
                Version = LocalBuildVersion;
            }

            // DIRECTORIES
            ArtifactsDir = RootDirectory / "artifacts";
            NugetRoot = ArtifactsDir / "nuget";
            NugetIntermediateRoot = RootDirectory / "build-intermediate" / "nuget";
            ZipRoot = ArtifactsDir / "zip";
            TestResultsRoot = ArtifactsDir / "test-results";
            BuildDirs = RootDirectory.GlobDirectories("**/bin")
                .Concat(RootDirectory.GlobDirectories("**/obj"))
                .Where(dir => !((string)dir).Contains("nukebuild"))
                .Concat(RootDirectory.GlobDirectories("**/node_modules"))
                .ToList();
            DirSuffix = Configuration;
            FileZipSuffix = Version + ".zip";
            ZipCoreArtifacts = ZipRoot / ("Avalonia-" + FileZipSuffix);
            ZipNuGetArtifacts = ZipRoot / ("Avalonia-NuGet-" + FileZipSuffix);
            ApiValidationSuppressionFiles = RootDirectory / "api";
            VersionOutputDir = b.VersionOutputDir;
        }

        string GetVersion()
        {
            var xdoc = XDocument.Load(RootDirectory / "build/SharedVersion.props");
            return xdoc.Descendants().First(x => x.Name.LocalName == "Version").Value;
        }
    }

}
