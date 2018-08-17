using System.Xml.Linq;
using System.Linq;

public class Parameters
{
    public string Configuration { get; private set; }
    public bool SkipTests { get; private set; }
    public string MainRepo { get; private set; }
    public string MasterBranch { get; private set; }
    public string ReleasePlatform { get; private set; }
    public string ReleaseConfiguration { get; private set; }
    public string MSBuildSolution { get; private set; }
    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }
    public bool IsRunningOnAppVeyor { get; private set; }
    public bool IsPullRequest { get; private set; }
    public bool IsMainRepo { get; private set; }
    public bool IsMasterBranch { get; private set; }
    public bool IsTagged { get; private set; }
    public bool IsReleasable { get; private set; }
    public bool IsMyGetRelease { get; private set; }
    public bool IsNuGetRelease { get; private set; }
    public string Version { get; private set; } 
    public DirectoryPath ArtifactsDir { get; private set; }
    public DirectoryPath NugetRoot { get; private set; }
    public DirectoryPath ZipRoot { get; private set; }
    public DirectoryPath BinRoot { get; private set; }
    public string DirSuffix { get; private set; }
    public DirectoryPathCollection BuildDirs { get; private set; }
    public string FileZipSuffix { get; private set; }
    public FilePath ZipCoreArtifacts { get; private set; }
    public FilePath ZipNuGetArtifacts { get; private set; }
    public DirectoryPath ZipSourceControlCatalogDesktopDirs { get; private set; }
    public FilePath ZipTargetControlCatalogDesktopDirs { get; private set; }

    public Parameters(ICakeContext context)
    {
        var buildSystem = context.BuildSystem();

        // ARGUMENTS
        Configuration = context.Argument("configuration", "Release");
        SkipTests = context.HasArgument("skip-tests");

        // CONFIGURATION
        MainRepo = "AvaloniaUI/Avalonia";
        MasterBranch = "master";
        ReleaseConfiguration = "Release";
        MSBuildSolution = "./dirs.proj";

        // PARAMETERS
        IsLocalBuild = buildSystem.IsLocalBuild;
        IsRunningOnUnix = context.IsRunningOnUnix();
        IsRunningOnWindows = context.IsRunningOnWindows();
        IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor;
        IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
        IsMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, buildSystem.AppVeyor.Environment.Repository.Name);
        IsMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch, buildSystem.AppVeyor.Environment.Repository.Branch);
        IsTagged = buildSystem.AppVeyor.Environment.Repository.Tag.IsTag 
                && !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name);
        IsReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, Configuration);
        IsMyGetRelease = !IsTagged && IsReleasable;

        // VERSION
        Version = context.Argument("force-nuget-version", GetVersion());

        if (IsRunningOnAppVeyor)
        {
            string tagVersion = null;
            if (IsTagged)
            {
                var tag = buildSystem.AppVeyor.Environment.Repository.Tag.Name;
                var nugetReleasePrefix = "nuget-release-";
                IsNuGetRelease = IsTagged && IsReleasable && tag.StartsWith(nugetReleasePrefix);
                if(IsNuGetRelease)
                    tagVersion = tag.Substring(nugetReleasePrefix.Length);
            }
            if(tagVersion != null)
            {
                Version = tagVersion;
            }
            else
            {
                // Use AssemblyVersion with Build as version
                Version += "-build" + context.EnvironmentVariable("APPVEYOR_BUILD_NUMBER") + "-beta";
            }
        }

        // DIRECTORIES
        ArtifactsDir = (DirectoryPath)context.Directory("./artifacts");
        NugetRoot = ArtifactsDir.Combine("nuget");
        ZipRoot = ArtifactsDir.Combine("zip");
        BinRoot = ArtifactsDir.Combine("bin");
        BuildDirs = context.GetDirectories("**/bin") + context.GetDirectories("**/obj");
        DirSuffix = Configuration;
        FileZipSuffix = Version + ".zip";
        ZipCoreArtifacts = ZipRoot.CombineWithFilePath("Avalonia-" + FileZipSuffix);
        ZipNuGetArtifacts = ZipRoot.CombineWithFilePath("Avalonia-NuGet-" + FileZipSuffix);
        ZipSourceControlCatalogDesktopDirs = (DirectoryPath)context.Directory("./samples/ControlCatalog.Desktop/bin/" + DirSuffix + "/net461");
        ZipTargetControlCatalogDesktopDirs = ZipRoot.CombineWithFilePath("ControlCatalog.Desktop-" + FileZipSuffix);
    }

    private static string GetVersion()
    {
        var xdoc = XDocument.Load("./build/SharedVersion.props");
        return xdoc.Descendants().First(x => x.Name.LocalName == "Version").Value;
    }
}
