using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.BuildServers;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;

public partial class Build
{
    [Parameter("configuration")]
    public string NukeArgConfiguration { get; set; }
    
    [Parameter("skip-tests")]
    public bool NukeArgSkipTests { get; set; }
    
    [Parameter("force-nuget-version")]
    public string NukeArgForceNugetVersion { get; set; }
    
    public class BuildParameters
    {
        public string Configuration { get; }
        public bool SkipTests { get; }
        public string MainRepo { get; }
        public string MasterBranch { get; }
        public string RepositoryName { get; }
        public string RepositoryBranch { get; }
        public string ReleaseConfiguration { get; }
        public string ReleaseBranchPrefix { get; }
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
        public string Version { get; }
        public AbsolutePath ArtifactsDir { get; }
        public AbsolutePath NugetRoot { get; }
        public AbsolutePath ZipRoot { get; }
        public AbsolutePath BinRoot { get; }
        public AbsolutePath TestResultsRoot { get; }
        public string DirSuffix { get; }
        public List<string> BuildDirs { get; }
        public string FileZipSuffix { get; }
        public AbsolutePath ZipCoreArtifacts { get; }
        public AbsolutePath ZipNuGetArtifacts { get; }
        public AbsolutePath ZipSourceControlCatalogDesktopDir { get; }
        public AbsolutePath ZipTargetControlCatalogDesktopDir { get; }


       public BuildParameters(Build b)
        {
            // ARGUMENTS
            Configuration = b.NukeArgConfiguration ?? "Release";
            SkipTests = b.NukeArgSkipTests;

            // CONFIGURATION
            MainRepo = "https://github.com/AvaloniaUI/Avalonia";
            MasterBranch = "refs/heads/master";
            ReleaseBranchPrefix = "refs/heads/release/";
            ReleaseConfiguration = "Release";
            MSBuildSolution = RootDirectory / "dirs.proj";

            // PARAMETERS
            IsLocalBuild = Host == HostType.Console;
            IsRunningOnUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                              Environment.OSVersion.Platform == PlatformID.MacOSX;
            IsRunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            IsRunningOnAzure = Host == HostType.TeamServices ||
                               Environment.GetEnvironmentVariable("LOGNAME") == "vsts";

            if (IsRunningOnAzure)
            {
                RepositoryName = TeamServices.Instance.RepositoryUri;
                RepositoryBranch = TeamServices.Instance.SourceBranch;
                IsPullRequest = TeamServices.Instance.PullRequestId.HasValue;
                IsMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, TeamServices.Instance.RepositoryUri);
            }
            IsMainRepo =
                StringComparer.OrdinalIgnoreCase.Equals(MainRepo,
                    RepositoryName);
            IsMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch,
                RepositoryBranch);
            IsReleaseBranch = RepositoryBranch?.StartsWith(ReleaseBranchPrefix, StringComparison.OrdinalIgnoreCase) ==
                              true;

            IsReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, Configuration);
            IsMyGetRelease = IsReleasable;
            IsNuGetRelease = IsMainRepo && IsReleasable && IsReleaseBranch;

            // VERSION
            Version = b.NukeArgForceNugetVersion ?? GetVersion();

            if (IsRunningOnAzure)
            {
                if (!IsNuGetRelease)
                {
                    // Use AssemblyVersion with Build as version
                    Version += "-build" + Environment.GetEnvironmentVariable("BUILD_BUILDID") + "-beta";
                }

                PublishTestResults = true;
            }

            // DIRECTORIES
            ArtifactsDir = RootDirectory / "artifacts";
            NugetRoot = ArtifactsDir / "nuget";
            ZipRoot = ArtifactsDir / "zip";
            BinRoot = ArtifactsDir / "bin";
            TestResultsRoot = ArtifactsDir / "test-results";
            BuildDirs = GlobDirectories(RootDirectory, "**bin").Concat(GlobDirectories(RootDirectory, "**obj")).ToList();
            DirSuffix = Configuration;
            FileZipSuffix = Version + ".zip";
            ZipCoreArtifacts = ZipRoot / ("Avalonia-" + FileZipSuffix);
            ZipNuGetArtifacts = ZipRoot / ("Avalonia-NuGet-" + FileZipSuffix);
            ZipSourceControlCatalogDesktopDir =
                RootDirectory / ("samples/ControlCatalog.Desktop/bin/" + DirSuffix + "/net461");
            ZipTargetControlCatalogDesktopDir = ZipRoot / ("ControlCatalog.Desktop-" + FileZipSuffix);
        }

        private static string GetVersion()
        {
            var xdoc = XDocument.Load("./build/SharedVersion.props");
            return xdoc.Descendants().First(x => x.Name.LocalName == "Version").Value;
        }
    }

}
