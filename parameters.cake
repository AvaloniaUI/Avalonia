public class Parameters
{
    public string Target { get; private set; }
    public string Platform { get; private set; }
    public string Configuration { get; private set; }
    public bool SkipTests { get; private set; }
    public string MainRepo { get; private set; }
    public string MasterBranch { get; private set; }
    public string AssemblyInfoPath { get; private set; }
    public string ReleasePlatform { get; private set; }
    public string ReleaseConfiguration { get; private set; }
    public string MSBuildSolution { get; private set; } 
    public string XBuildSolution { get; private set; } 
    public bool IsPlatformAnyCPU { get; private set; }
    public bool IsPlatformX86 { get; private set; }
    public bool IsPlatformX64 { get; private set; }
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
    public DirectoryPath TestsRoot { get; private set; }
    public string DirSuffix { get; private set; }
    public string DirSuffixSkia { get; private set; }
    public string DirSuffixIOS { get; private set; }
    public DirectoryPathCollection BuildDirs { get; private set; }
    public string FileZipSuffix { get; private set; }
    public FilePath ZipCoreArtifacts { get; private set; }
    public DirectoryPath ZipSourceControlCatalogDesktopDirs { get; private set; }
    public FilePath ZipTargetControlCatalogDesktopDirs { get; private set; }

    public Parameters(ICakeContext context)
    {
        var buildSystem = context.BuildSystem();

        // ARGUMENTS
        Target = context.Argument("target", "Default");
        Platform = context.Argument("platform", "Any CPU");
        Configuration = context.Argument("configuration", "Release");
        SkipTests = context.HasArgument("skip-tests");

        // CONFIGURATION
        MainRepo = "AvaloniaUI/Avalonia";
        MasterBranch = "master";
        AssemblyInfoPath = context.File("./src/Shared/SharedAssemblyInfo.cs");
        ReleasePlatform = "Any CPU";
        ReleaseConfiguration = "Release";
        MSBuildSolution = "./Avalonia.sln";
        XBuildSolution = "./Avalonia.XBuild.sln";

        // PARAMETERS
        IsPlatformAnyCPU = StringComparer.OrdinalIgnoreCase.Equals(Platform, "Any CPU");
        IsPlatformX86 = StringComparer.OrdinalIgnoreCase.Equals(Platform, "x86");
        IsPlatformX64 = StringComparer.OrdinalIgnoreCase.Equals(Platform, "x64");
        IsLocalBuild = buildSystem.IsLocalBuild;
        IsRunningOnUnix = context.IsRunningOnUnix();
        IsRunningOnWindows = context.IsRunningOnWindows();
        IsRunningOnAppVeyor = buildSystem.AppVeyor.IsRunningOnAppVeyor;
        IsPullRequest = buildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
        IsMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, buildSystem.AppVeyor.Environment.Repository.Name);
        IsMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch, buildSystem.AppVeyor.Environment.Repository.Branch);
        IsTagged = buildSystem.AppVeyor.Environment.Repository.Tag.IsTag 
                && !string.IsNullOrWhiteSpace(buildSystem.AppVeyor.Environment.Repository.Tag.Name);
        IsReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleasePlatform, Platform) 
                    && StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, Configuration);
        IsMyGetRelease = !IsTagged && IsReleasable;
        

        // VERSION
        Version = context.Argument("force-nuget-version", context.ParseAssemblyInfo(AssemblyInfoPath).AssemblyVersion);

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
                Version += "-build" + context.EnvironmentVariable("APPVEYOR_BUILD_NUMBER") + "-alpha";
            }
        }

        // DIRECTORIES
        ArtifactsDir = (DirectoryPath)context.Directory("./artifacts");
        NugetRoot = ArtifactsDir.Combine("nuget");
        ZipRoot = ArtifactsDir.Combine("zip");
        BinRoot = ArtifactsDir.Combine("bin");
        TestsRoot = ArtifactsDir.Combine("tests");

        DirSuffix = Configuration;
        DirSuffixSkia = (IsPlatformAnyCPU ? "x86" : Platform) + "/" + Configuration;
        DirSuffixIOS = "iPhone" + "/" + Configuration;

        BuildDirs = 
            context.GetDirectories("./src/**/bin/" + DirSuffix) + 
            context.GetDirectories("./src/**/obj/" + DirSuffix) + 
            context.GetDirectories("./src/Markup/**/bin/" + DirSuffix) + 
            context.GetDirectories("./src/Markup/**/obj/" + DirSuffix) + 
            context.GetDirectories("./src/Android/**/bin/" + DirSuffix) + 
            context.GetDirectories("./src/Android/**/obj/" + DirSuffix) + 
            context.GetDirectories("./src/Gtk/**/bin/" + DirSuffix) + 
            context.GetDirectories("./src/Gtk/**/obj/" + DirSuffix) + 
            context.GetDirectories("./src/iOS/**/bin/" + DirSuffixIOS) + 
            context.GetDirectories("./src/iOS/**/obj/" + DirSuffixIOS) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Android/bin/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Android/obj/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Android.TestApp/bin/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Android.TestApp/obj/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Desktop/bin/" + DirSuffixSkia) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Desktop/obj/" + DirSuffixSkia) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Desktop.NetStandard/bin/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.Desktop.NetStandard/obj/" + DirSuffix) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + DirSuffixIOS) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.iOS/obj/" + DirSuffixIOS) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.iOS.TestApp/bin/" + DirSuffixIOS) + 
            (DirectoryPath)context.Directory("./src/Skia/Avalonia.Skia.iOS.TestApp/obj/" + DirSuffixIOS) + 
            context.GetDirectories("./src/Windows/**/bin/" + DirSuffix) + 
            context.GetDirectories("./src/Windows/**/obj/" + DirSuffix) + 
            context.GetDirectories("./tests/**/bin/" + DirSuffix) + 
            context.GetDirectories("./tests/**/obj/" + DirSuffix) + 
            context.GetDirectories("./Samples/**/bin/" + DirSuffix) + 
            context.GetDirectories("./Samples/**/obj/" + DirSuffix);

        FileZipSuffix = Version + ".zip";
        ZipCoreArtifacts = ZipRoot.CombineWithFilePath("Avalonia-" + FileZipSuffix);
        ZipSourceControlCatalogDesktopDirs = (DirectoryPath)context.Directory("./samples/ControlCatalog.Desktop/bin/" + DirSuffix);
        ZipTargetControlCatalogDesktopDirs = ZipRoot.CombineWithFilePath("ControlCatalog.Desktop-" + FileZipSuffix);
    }
}
