///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=4.2.0"

///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=xunit.runner.console&version=2.1.0"

///////////////////////////////////////////////////////////////////////////////
// USINGS
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using Polly;

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var platform = Argument("platform", "Any CPU");
var configuration = Argument("configuration", "Release");

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
var binRoot = artifactsDir.Combine("bin");
var zipBinArtifacts = artifactsDir.CombineWithFilePath("Avalonia-" + version + ".zip");

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

///////////////////////////////////////////////////////////////////////////////
// NUGET NUSPECS
///////////////////////////////////////////////////////////////////////////////

var SerilogVersion = "1.5.14";
var SplatVersion = "1.6.2";
var SpracheVersion = "2.0.0.50";
var SystemReactiveVersion = "3.0.0";
var SkiaSharpVersion = "1.53.0";
var SharpDXVersion = "3.0.2";
var SharpDXDirect2D1Version = "3.0.2";
var SharpDXDXGIVersion = "3.0.2";

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
    new [] { "./src/", "Avalonia.SceneGraph", ".dll" },
    new [] { "./src/", "Avalonia.SceneGraph", ".xml" },
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
        Files = coreLibrariesNuSpecContent.ToList(),
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
    CleanDirectory(binRoot);
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
    .Does(() =>
{
    var pattern = "./tests/Avalonia.*.UnitTests/bin/" + dirSuffix + "/Avalonia.*.UnitTests.dll";

    Func<IFileSystemInfo, bool> ExcludeWindowsTests = i => {
        return !(i.Path.FullPath.IndexOf("Direct2D", StringComparison.OrdinalIgnoreCase) >= 0);
    };

    var unitTests = isRunningOnWindows ? GetFiles(pattern) : GetFiles(pattern, ExcludeWindowsTests);

    if (isRunningOnWindows)
    {
        var windowsTests = GetFiles("./tests/Avalonia.DesignerSupport.Tests/bin/" + dirSuffix + "/*Tests.dll") + 
                           GetFiles("./tests/Avalonia.LeakTests/bin/" + dirSuffix + "/*Tests.dll") + 
                           GetFiles("./tests/Avalonia.RenderTests/bin/" + dirSuffix + "/*Tests.dll");

        unitTests += windowsTests;
    }

    var toolPath = (isPlatformAnyCPU || isPlatformX86) ? 
        "./tools/xunit.runner.console/tools/xunit.console.x86.exe" :
        "./tools/xunit.runner.console/tools/xunit.console.exe";

    var settings = new XUnit2Settings 
    { 
        ToolPath = toolPath,
        Parallelism = ParallelismOption.None 
    };

    if (isRunningOnWindows)
    {
        settings.NoAppDomain = false;
    }

    foreach (var file in unitTests)
    {
        Information("Running test " + file.GetFilenameWithoutExtension());
        XUnit2(file.FullPath, settings);
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
    Zip(binRoot, zipBinArtifacts);
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

Task("Upload-AppVeyor-Artifacts")
    .IsDependentOn("Zip-Files")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => isRunningOnAppVeyor)
    .Does(() =>
{
    AppVeyor.UploadArtifact(zipBinArtifacts.FullPath);

    foreach(var nupkg in nugetPackages)
    {
        AppVeyor.UploadArtifact(nupkg.FullPath);
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
  .IsDependentOn("Upload-AppVeyor-Artifacts")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet");

Task("Travis")
  .IsDependentOn("Run-Unit-Tests");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

RunTarget(target);
