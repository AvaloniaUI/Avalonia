///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=4.2.0"
#addin "nuget:?package=NuGet.Core&version=2.12.0"

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=xunit.runner.console&version=2.1.0"
#tool "nuget:?package=OpenCover"

///////////////////////////////////////////////////////////////////////////////
// USINGS
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Polly;
using NuGet;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var platform = Argument("platform", "Any CPU");
var configuration = Argument("configuration", "Release");
var skipTests = HasArgument("skip-tests");

///////////////////////////////////////////////////////////////////////////////
// CONFIGURATION
///////////////////////////////////////////////////////////////////////////////

var MainRepo = "AvaloniaUI/Avalonia";
var MasterBranch = "master";
var AssemblyInfoPath = File("./src/Shared/SharedAssemblyInfo.cs");
var ReleasePlatform = "Any CPU";
var ReleaseConfiguration = "Release";
var MSBuildSolution = "./Avalonia.sln";
var XBuildSolution = "./Avalonia.sln";

///////////////////////////////////////////////////////////////////////////////
// PARAMETERS
///////////////////////////////////////////////////////////////////////////////

var isPlatformAnyCPU = StringComparer.OrdinalIgnoreCase.Equals(platform, "Any CPU");
var isPlatformX86 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x86");
var isPlatformX64 = StringComparer.OrdinalIgnoreCase.Equals(platform, "x64");
var isLocalBuild = BuildSystem.IsLocalBuild;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();
var isRunningOnAppVeyor = BuildSystem.AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = BuildSystem.AppVeyor.Environment.PullRequest.IsPullRequest;
var isMainRepo = StringComparer.OrdinalIgnoreCase.Equals(MainRepo, BuildSystem.AppVeyor.Environment.Repository.Name);
var isMasterBranch = StringComparer.OrdinalIgnoreCase.Equals(MasterBranch, BuildSystem.AppVeyor.Environment.Repository.Branch);
var isTagged = BuildSystem.AppVeyor.Environment.Repository.Tag.IsTag 
               && !string.IsNullOrWhiteSpace(BuildSystem.AppVeyor.Environment.Repository.Tag.Name);
var isReleasable = StringComparer.OrdinalIgnoreCase.Equals(ReleasePlatform, platform) 
                   && StringComparer.OrdinalIgnoreCase.Equals(ReleaseConfiguration, configuration);
var isMyGetRelease = !isTagged && isReleasable;
var isNuGetRelease = isTagged && isReleasable;

///////////////////////////////////////////////////////////////////////////////
// VERSION
///////////////////////////////////////////////////////////////////////////////

var version = ParseAssemblyInfo(AssemblyInfoPath).AssemblyVersion;

if (isRunningOnAppVeyor)
{
    if (isTagged)
    {
        // Use Tag Name as version
        version = BuildSystem.AppVeyor.Environment.Repository.Tag.Name;
    }
    else
    {
        // Use AssemblyVersion with Build as version
        version += "-build" + EnvironmentVariable("APPVEYOR_BUILD_NUMBER") + "-alpha";
    }
}

///////////////////////////////////////////////////////////////////////////////
// DIRECTORIES
///////////////////////////////////////////////////////////////////////////////

var artifactsDir = (DirectoryPath)Directory("./artifacts");
var nugetRoot = artifactsDir.Combine("nuget");
var zipRoot = artifactsDir.Combine("zip");
var binRoot = artifactsDir.Combine("bin");
var testsRoot = artifactsDir.Combine("tests");

var dirSuffix = configuration;
var dirSuffixSkia = (isPlatformAnyCPU ? "x86" : platform) + "/" + configuration;
var dirSuffixIOS = "iPhone" + "/" + configuration;

var buildDirs = 
    GetDirectories("./src/**/bin/" + dirSuffix) + 
    GetDirectories("./src/**/obj/" + dirSuffix) + 
    GetDirectories("./src/Markup/**/bin/" + dirSuffix) + 
    GetDirectories("./src/Markup/**/obj/" + dirSuffix) + 
    GetDirectories("./src/Android/**/bin/" + dirSuffix) + 
    GetDirectories("./src/Android/**/obj/" + dirSuffix) + 
    GetDirectories("./src/Gtk/**/bin/" + dirSuffix) + 
    GetDirectories("./src/Gtk/**/obj/" + dirSuffix) + 
    GetDirectories("./src/iOS/**/bin/" + dirSuffixIOS) + 
    GetDirectories("./src/iOS/**/obj/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Android/bin/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Android/obj/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Android.TestApp/bin/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Android.TestApp/obj/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Desktop/bin/" + dirSuffixSkia) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Desktop/obj/" + dirSuffixSkia) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS/obj/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS.TestApp/bin/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS.TestApp/obj/" + dirSuffixIOS) + 
    GetDirectories("./src/Windows/**/bin/" + dirSuffix) + 
    GetDirectories("./src/Windows/**/obj/" + dirSuffix) + 
    GetDirectories("./tests/**/bin/" + dirSuffix) + 
    GetDirectories("./tests/**/obj/" + dirSuffix) + 
    GetDirectories("./Samples/**/bin/" + dirSuffix) + 
    GetDirectories("./Samples/**/obj/" + dirSuffix);

var fileZipSuffix = version + ".zip";
var zipCoreArtifacts = zipRoot.CombineWithFilePath("Avalonia-" + fileZipSuffix);
var zipSourceControlCatalogDesktopDirs = (DirectoryPath)Directory("./samples/ControlCatalog.Desktop/bin/" + dirSuffix);
var zipTargetControlCatalogDesktopDirs = zipRoot.CombineWithFilePath("ControlCatalog.Desktop-" + fileZipSuffix);

///////////////////////////////////////////////////////////////////////////////
// NUGET NUSPECS
///////////////////////////////////////////////////////////////////////////////

Information("Getting git modules:");

var ignoredSubModulesPaths = System.IO.File.ReadAllLines(".git/config").Where(m=>m.StartsWith("[submodule ")).Select(m => 
{
    var path = m.Split(' ')[1].Trim("\"[] \t".ToArray());
    Information(path);
    return ((DirectoryPath)Directory(path)).FullPath;
}).ToList();

var normalizePath = new Func<string, string>(
    path => path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).ToUpperInvariant());

// Key: Package Id
// Value is Tuple where Item1: Package Version, Item2: The packages.config file path.
var packageVersions = new Dictionary<string, IList<Tuple<string,string>>>();

System.IO.Directory.EnumerateFiles(((DirectoryPath)Directory("./src")).FullPath, "packages.config", SearchOption.AllDirectories).ToList().ForEach(fileName =>
{
    if (!ignoredSubModulesPaths.Any(i => normalizePath(fileName).Contains(normalizePath(i))))
    {
        var file = new PackageReferenceFile(fileName);
        foreach (PackageReference packageReference in file.GetPackageReferences())
        {
            IList<Tuple<string, string>> versions;
            packageVersions.TryGetValue(packageReference.Id, out versions);
            if (versions == null)
            {
                versions = new List<Tuple<string, string>>();
                packageVersions[packageReference.Id] = versions;
            }
            versions.Add(Tuple.Create(packageReference.Version.ToString(), fileName));
        }
    }
});

Information("Checking installed NuGet package dependencies versions:");

packageVersions.ToList().ForEach(package =>
{
    var packageVersion = package.Value.First().Item1;
    bool isValidVersion = package.Value.All(x => x.Item1 == packageVersion);
    if (!isValidVersion)
    {
        Information("Error: package {0} has multiple versions installed:", package.Key);
        foreach (var v in package.Value)
        {
            Information("{0}, file: {1}", v.Item1, v.Item2);
        }
        throw new Exception("Detected multiple NuGet package version installed for different projects.");
    }
});

Information("Setting NuGet package dependencies versions:");

var SerilogVersion = packageVersions["Serilog"].FirstOrDefault().Item1;
var SplatVersion = packageVersions["Splat"].FirstOrDefault().Item1;
var SpracheVersion = packageVersions["Sprache"].FirstOrDefault().Item1;
var SystemReactiveVersion = packageVersions["System.Reactive"].FirstOrDefault().Item1;
var SkiaSharpVersion = packageVersions["SkiaSharp"].FirstOrDefault().Item1;
var SharpDXVersion = packageVersions["SharpDX"].FirstOrDefault().Item1;
var SharpDXDirect2D1Version = packageVersions["SharpDX.Direct2D1"].FirstOrDefault().Item1;
var SharpDXDXGIVersion = packageVersions["SharpDX.DXGI"].FirstOrDefault().Item1;

Information("Package: Serilog, version: {0}", SerilogVersion);
Information("Package: Splat, version: {0}", SplatVersion);
Information("Package: Sprache, version: {0}", SpracheVersion);
Information("Package: System.Reactive, version: {0}", SystemReactiveVersion);
Information("Package: SkiaSharp, version: {0}", SkiaSharpVersion);
Information("Package: SharpDX, version: {0}", SharpDXVersion);
Information("Package: SharpDX.Direct2D1, version: {0}", SharpDXDirect2D1Version);
Information("Package: SharpDX.DXGI, version: {0}", SharpDXDXGIVersion);

var SetNuGetNuspecCommonProperties = new Action<NuGetPackSettings> ((nuspec) => {
    nuspec.Version = version;
    nuspec.Authors = new [] { "Avalonia Team" };
    nuspec.Owners = new [] { "stevenk" };
    nuspec.LicenseUrl = new Uri("http://opensource.org/licenses/MIT");
    nuspec.ProjectUrl = new Uri("https://github.com/AvaloniaUI/Avalonia/");
    nuspec.RequireLicenseAcceptance = false;
    nuspec.Symbols = false;
    nuspec.NoPackageAnalysis = true;
    nuspec.Description = "The Avalonia UI framework";
    nuspec.Copyright = "Copyright 2015";
    nuspec.Tags = new [] { "Avalonia" };
});

var coreLibraries = new string[][]
{
    new [] { "./src/", "Avalonia.Animation", ".dll" },
    new [] { "./src/", "Avalonia.Animation", ".xml" },
    new [] { "./src/", "Avalonia.Base", ".dll" },
    new [] { "./src/", "Avalonia.Base", ".xml" },
    new [] { "./src/", "Avalonia.Controls", ".dll" },
    new [] { "./src/", "Avalonia.Controls", ".xml" },
    new [] { "./src/", "Avalonia.DesignerSupport", ".dll" },
    new [] { "./src/", "Avalonia.DesignerSupport", ".xml" },
    new [] { "./src/", "Avalonia.Diagnostics", ".dll" },
    new [] { "./src/", "Avalonia.Diagnostics", ".xml" },
    new [] { "./src/", "Avalonia.Input", ".dll" },
    new [] { "./src/", "Avalonia.Input", ".xml" },
    new [] { "./src/", "Avalonia.Interactivity", ".dll" },
    new [] { "./src/", "Avalonia.Interactivity", ".xml" },
    new [] { "./src/", "Avalonia.Layout", ".dll" },
    new [] { "./src/", "Avalonia.Layout", ".xml" },
    new [] { "./src/", "Avalonia.Logging.Serilog", ".dll" },
    new [] { "./src/", "Avalonia.Logging.Serilog", ".xml" },
    new [] { "./src/", "Avalonia.Visuals", ".dll" },
    new [] { "./src/", "Avalonia.Visuals", ".xml" },
    new [] { "./src/", "Avalonia.Styling", ".dll" },
    new [] { "./src/", "Avalonia.Styling", ".xml" },
    new [] { "./src/", "Avalonia.ReactiveUI", ".dll" },
    new [] { "./src/", "Avalonia.Themes.Default", ".dll" },
    new [] { "./src/", "Avalonia.Themes.Default", ".xml" },
    new [] { "./src/Markup/", "Avalonia.Markup", ".dll" },
    new [] { "./src/Markup/", "Avalonia.Markup", ".xml" },
    new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".dll" },
    new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".xml" }
};

var coreLibrariesFiles = coreLibraries.Select((lib) => {
    return (FilePath)File(lib[0] + lib[1] + "/bin/" + dirSuffix + "/" + lib[1] + lib[2]);
}).ToList();

var coreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
    return new NuSpecContent { 
        Source = file.FullPath, Target = "lib/portable-windows8+net45" 
    };
});

var win32CoreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
    return new NuSpecContent { 
        Source = file.FullPath, Target = "lib/net45" 
    };
});

var net45RuntimePlatformExtensions = new [] {".xml", ".dll"};
var net45RuntimePlatform = net45RuntimePlatformExtensions.Select(libSuffix => {
    return new NuSpecContent {
        Source = ((FilePath)File("./src/Avalonia.DotNetFrameworkRuntime/bin/" + dirSuffix + "/Avalonia.DotNetFrameworkRuntime" + libSuffix)).FullPath, 
        Target = "lib/net45" 
    };
});

var nuspecNuGetSettingsCore = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Serilog", Version = SerilogVersion },
            new NuSpecDependency() { Id = "Splat", Version = SplatVersion },
            new NuSpecDependency() { Id = "Sprache", Version = SpracheVersion },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = coreLibrariesNuSpecContent.Concat(win32CoreLibrariesNuSpecContent).Concat(net45RuntimePlatform).ToList(),
        BasePath = Directory("./"),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.HtmlRenderer
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.HtmlRenderer",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.HtmlRenderer.dll", Target = "lib/portable-windows8+net45" }
        },
        BasePath = Directory("./src/Avalonia.HtmlRenderer/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    }
};

var nuspecNuGetSettingsMobile = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Android
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Android",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Skia.Android", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Android.dll", Target = "lib/MonoAndroid10" }
        },
        BasePath = Directory("./src/Android/Avalonia.Android/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Skia.Android
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Skia.Android",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Skia.Android.dll", Target = "lib/MonoAndroid10" }
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.Android/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.iOS
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.iOS",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Skia.iOS", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.iOS.dll", Target = "lib/Xamarin.iOS10" }
        },
        BasePath = Directory("./src/iOS/Avalonia.iOS/bin/" + dirSuffixIOS),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Skia.iOS
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Skia.iOS",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Skia.iOS.dll", Target = "lib/Xamarin.iOS10" }
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + dirSuffixIOS),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Mobile
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Mobile",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Android", Version = version },
            new NuSpecDependency() { Id = "Avalonia.iOS", Version = version }
        },
        Files = new NuSpecContent[]
        {
            new NuSpecContent { Source = "licence.md", Target = "" }
        },
        BasePath = Directory("./"),
        OutputDirectory = nugetRoot
    }
};

var nuspecNuGetSettingsDesktop = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Win32
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Win32",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Win32.dll", Target = "lib/net45" }
        },
        BasePath = Directory("./src/Windows/Avalonia.Win32/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Direct2D1
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Direct2D1",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "SharpDX", Version = SharpDXVersion },
            new NuSpecDependency() { Id = "SharpDX.Direct2D1", Version = SharpDXDirect2D1Version },
            new NuSpecDependency() { Id = "SharpDX.DXGI", Version = SharpDXDXGIVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Direct2D1.dll", Target = "lib/net45" }
        },
        BasePath = Directory("./src/Windows/Avalonia.Direct2D1/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Gtk
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Gtk",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Gtk.dll", Target = "lib/net45" }
        },
        BasePath = Directory("./src/Gtk/Avalonia.Gtk/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Cairo
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Cairo",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Cairo.dll", Target = "lib/net45" }
        },
        BasePath = Directory("./src/Gtk/Avalonia.Cairo/bin/" + dirSuffix),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Skia.Desktop
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Skia.Desktop",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia", Version = version },
            new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Skia.Desktop.dll", Target = "lib/net45" }
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.Desktop/bin/" + dirSuffixSkia),
        OutputDirectory = nugetRoot
    },
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia.Desktop
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Desktop",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Win32", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Direct2D1", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Gtk", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Cairo", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Skia.Desktop", Version = version }
        },
        Files = new NuSpecContent[]
        {
            new NuSpecContent { Source = "licence.md", Target = "" }
        },
        BasePath = Directory("./"),
        OutputDirectory = nugetRoot
    }
};

var nuspecNuGetSettings = new List<NuGetPackSettings>();

nuspecNuGetSettings.AddRange(nuspecNuGetSettingsCore);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsDesktop);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsMobile);

nuspecNuGetSettings.ForEach((nuspec) => SetNuGetNuspecCommonProperties(nuspec));

var nugetPackages = nuspecNuGetSettings.Select(nuspec => {
    return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
}).ToArray();

var binFiles = nuspecNuGetSettings.SelectMany(nuspec => {
    return nuspec.Files.Select(file => {
        return ((DirectoryPath)nuspec.BasePath).CombineWithFilePath(file.Source);
    });
}).GroupBy(f => f.FullPath).Select(g => g.First());

///////////////////////////////////////////////////////////////////////////////
// INFORMATION
///////////////////////////////////////////////////////////////////////////////

Information("Building version {0} of Avalonia ({1}, {2}, {3}) using version {4} of Cake.", 
    version,
    platform,
    configuration,
    target,
    typeof(ICakeContext).Assembly.GetName().Version.ToString());

if (isRunningOnAppVeyor)
{
    Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
    Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
}

Information("Target: " + target);
Information("Platform: " + platform);
Information("Configuration: " + configuration);
Information("IsLocalBuild: " + isLocalBuild);
Information("IsRunningOnUnix: " + isRunningOnUnix);
Information("IsRunningOnWindows: " + isRunningOnWindows);
Information("IsRunningOnAppVeyor: " + isRunningOnAppVeyor);
Information("IsPullRequest: " + isPullRequest);
Information("IsMainRepo: " + isMainRepo);
Information("IsMasterBranch: " + isMasterBranch);
Information("IsTagged: " + isTagged);
Information("IsReleasable: " + isReleasable);
Information("IsMyGetRelease: " + isMyGetRelease);
Information("IsNuGetRelease: " + isNuGetRelease);

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories(buildDirs);
    CleanDirectory(artifactsDir);
    CleanDirectory(nugetRoot);
    CleanDirectory(zipRoot);
    CleanDirectory(binRoot);
    CleanDirectory(testsRoot);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var maxRetryCount = 5;
    var toolTimeout = 1d;
    Policy
        .Handle<Exception>()
        .Retry(maxRetryCount, (exception, retryCount, context) => {
            if (retryCount == maxRetryCount)
            {
                throw exception;
            }
            else
            {
                Verbose("{0}", exception);
                toolTimeout+=0.5;
            }})
        .Execute(()=> {
            if(isRunningOnWindows)
            {
                NuGetRestore(MSBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
            }
            else
            {
                NuGetRestore(XBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
            }
        });
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(isRunningOnWindows)
    {
        MSBuild(MSBuildSolution, settings => {
            settings.SetConfiguration(configuration);
            settings.WithProperty("Platform", "\"" + platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("Windows", "True");
            settings.UseToolVersion(MSBuildToolVersion.VS2015);
            settings.SetNodeReuse(false);
        });
    }
    else
    {
        XBuild(XBuildSolution, settings => {
            settings.SetConfiguration(configuration);
            settings.WithProperty("Platform", "\"" + platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
        });
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .WithCriteria(() => !skipTests)
    .Does(() =>
{
    var unitTests = GetDirectories("./tests/Avalonia.*.UnitTests")
        .Select(dir => System.IO.Path.GetFileName(dir.FullPath))
        .Where(name => isRunningOnWindows ? true : !(name.IndexOf("Direct2D", StringComparison.OrdinalIgnoreCase) >= 0))
        .Select(name => MakeAbsolute(File("./tests/" + name + "/bin/" + dirSuffix + "/" + name + ".dll")))
        .ToList();

    if (isRunningOnWindows)
    {
        var leakTests = GetFiles("./tests/Avalonia.LeakTests/bin/" + dirSuffix + "/*.LeakTests.dll");

        unitTests.AddRange(leakTests);
    }

    var toolPath = (isPlatformAnyCPU || isPlatformX86) ? 
        "./tools/xunit.runner.console/tools/xunit.console.x86.exe" :
        "./tools/xunit.runner.console/tools/xunit.console.exe";

    var xUnitSettings = new XUnit2Settings 
    { 
        ToolPath = toolPath,
        Parallelism = ParallelismOption.None,
        ShadowCopy = false
    };

    xUnitSettings.NoAppDomain = !isRunningOnWindows;

    var openCoverOutput = artifactsDir.GetFilePath(new FilePath("./coverage.xml"));
    var openCoverSettings = new OpenCoverSettings()
        .WithFilter("+[Avalonia.*]* -[*Test*]* -[ControlCatalog*]*")
        .WithFilter("-[Avalonia.*]OmniXaml.* -[Avalonia.*]Glass.*")
        .WithFilter("-[Avalonia.HtmlRenderer]TheArtOfDev.HtmlRenderer.* +[Avalonia.HtmlRenderer]TheArtOfDev.HtmlRenderer.Avalonia.* -[Avalonia.ReactiveUI]*");
    
    openCoverSettings.ReturnTargetCodeOffset = 0;

    foreach(var test in unitTests.Where(testFile => FileExists(testFile)))
    {
        CopyDirectory(test.GetDirectory(), testsRoot);
    }

    var testsInDirectoryToRun = new List<FilePath>();
    if(isRunningOnWindows)
    {
        testsInDirectoryToRun.AddRange(GetFiles("./artifacts/tests/*Tests.dll"));
    }
    else
    {
        testsInDirectoryToRun.AddRange(GetFiles("./artifacts/tests/*.UnitTests.dll"));
    }

    if(isRunningOnWindows)
    {
        OpenCover(context => {
            context.XUnit2(testsInDirectoryToRun, xUnitSettings);
        }, openCoverOutput, openCoverSettings);
    }
    else
    {
        XUnit2(testsInDirectoryToRun, xUnitSettings);
    }
});

Task("Copy-Files")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    CopyFiles(binFiles, binRoot);
});

Task("Zip-Files")
    .IsDependentOn("Copy-Files")
    .Does(() =>
{
    Zip(binRoot, zipCoreArtifacts);

    Zip(zipSourceControlCatalogDesktopDirs, 
        zipTargetControlCatalogDesktopDirs, 
        GetFiles(zipSourceControlCatalogDesktopDirs.FullPath + "/*.dll") + 
        GetFiles(zipSourceControlCatalogDesktopDirs.FullPath + "/*.exe"));
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    foreach(var nuspec in nuspecNuGetSettings)
    {
        NuGetPack(nuspec);
    }
});

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isMyGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("MYGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API key.");
    }

    var apiUrl = EnvironmentVariable("MYGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve MyGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            Source = apiUrl,
            ApiKey = apiKey
        });
    }
})
.OnError(exception =>
{
    Information("Publish-MyGet Task failed, but continuing with next Task...");
});

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !isLocalBuild)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isMainRepo)
    .WithCriteria(() => isMasterBranch)
    .WithCriteria(() => isNuGetRelease)
    .Does(() =>
{
    var apiKey = EnvironmentVariable("NUGET_API_KEY");
    if(string.IsNullOrEmpty(apiKey)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API key.");
    }

    var apiUrl = EnvironmentVariable("NUGET_API_URL");
    if(string.IsNullOrEmpty(apiUrl)) 
    {
        throw new InvalidOperationException("Could not resolve NuGet API url.");
    }

    foreach(var nupkg in nugetPackages)
    {
        NuGetPush(nupkg, new NuGetPushSettings {
            ApiKey = apiKey,
            Source = apiUrl
        });
    }
})
.OnError(exception =>
{
    Information("Publish-NuGet Task failed, but continuing with next Task...");
});

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
  .IsDependentOn("Create-NuGet-Packages");

Task("Default")
  .IsDependentOn("Package");

Task("AppVeyor")
  .IsDependentOn("Zip-Files")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet");

Task("Travis")
  .IsDependentOn("Run-Unit-Tests");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
