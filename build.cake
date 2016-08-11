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
var ReleasePlatform = "AnyCPU";
var ReleaseConfiguration = "Release";
var MSBuildSolution = "./Avalonia.sln";
var XBuildSolution = "./Avalonia.sln";

///////////////////////////////////////////////////////////////////////////////
// PARAMETERS
///////////////////////////////////////////////////////////////////////////////

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
var isAnyCPU = StringComparer.OrdinalIgnoreCase.Equals(platform, "AnyCPU");

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
var testResultsDir = artifactsDir.Combine("test-results");
var nugetRoot = artifactsDir.Combine("nuget");

var dirSuffix = configuration;
var dirSuffixSkia = (isAnyCPU ? "x86" : platform) + "/" + configuration;
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
    (DirectoryPath)Directory("./obj/Skia/Avalonia.Skia.Android/obj/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Android.TestApp/bin/" + dirSuffix) + 
    (DirectoryPath)Directory("./obj/Skia/Avalonia.Skia.Android.TestApp/obj/" + dirSuffix) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.Desktop/bin/" + dirSuffixSkia) + 
    (DirectoryPath)Directory("./obj/Skia/Avalonia.Skia.Desktop/obj/" + dirSuffixSkia) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./obj/Skia/Avalonia.Skia.iOS/obj/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./src/Skia/Avalonia.Skia.iOS.TestApp/bin/" + dirSuffixIOS) + 
    (DirectoryPath)Directory("./obj/Skia/Avalonia.Skia.iOS.TestApp/obj/" + dirSuffixIOS) + 
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

var nuspecNuGetSettingsCore = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Animation
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Animation",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Animation.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Animation.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Animation/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Animation")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Base
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Base",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Base.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Base.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Base/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Base")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Controls
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Controls",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Controls.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Controls.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Controls/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Controls")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.DesignerSupport
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.DesignerSupport",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup.Xaml", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Themes.Default", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.DesignerSupport.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.DesignerSupport.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.DesignerSupport/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.DesignerSupport")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Diagnostics
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Diagnostics",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup.Xaml", Version = version },
            new NuSpecDependency() { Id = "Avalonia.ReactiveUI", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Themes.Default", Version = version },
            new NuSpecDependency() { Id = "Splat", Version = SplatVersion },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Diagnostics.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Diagnostics.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Diagnostics/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Diagnostics")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.HtmlRenderer
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.HtmlRenderer",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "System.Reactive.Core", Version = SystemReactiveVersion },
            new NuSpecDependency() { Id = "System.Reactive.Interfaces", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.HtmlRenderer.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.HtmlRenderer.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.HtmlRenderer/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.HtmlRenderer")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Input
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Input",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Input.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Input.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Input/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Input")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Interactivity
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Interactivity",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Interactivity.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Interactivity.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Interactivity/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Interactivity")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Layout
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Layout",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Layout.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Layout.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Layout/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Layout")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Logging.Serilog
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Logging.Serilog",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Serilog", Version = SerilogVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Logging.Serilog.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Logging.Serilog.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Logging.Serilog/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Logging.Serilog")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.ReactiveUI
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.ReactiveUI",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Splat", Version = SplatVersion },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.ReactiveUI.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.ReactiveUI.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.ReactiveUI/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.ReactiveUI")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.SceneGraph
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.SceneGraph",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.SceneGraph.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.SceneGraph.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.SceneGraph/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.SceneGraph")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Styling
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Styling",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Styling.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Styling.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Styling/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Styling")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src: Avalonia.Themes.Default
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Themes.Default",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup.Xaml", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Themes.Default.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Themes.Default.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Avalonia.Themes.Default/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Themes.Default")
    }
};

var nuspecNuGetSettingsMarkup = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/Markup: Avalonia.Markup
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Markup",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Markup.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Markup.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Markup/Avalonia.Markup/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Markup")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src/Markup: Avalonia.Markup.Xaml
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia.Markup.Xaml",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "Sprache", Version = SpracheVersion },
            new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion }
        },
        Files = new []
        {
            new NuSpecContent { Source = "Avalonia.Markup.Xaml.dll", Target = "lib/portable-windows8+net45" },
            new NuSpecContent { Source = "Avalonia.Markup.Xaml.xml", Target = "lib/portable-windows8+net45" },
        },
        BasePath = Directory("./src/Markup/Avalonia.Markup.Xaml/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Markup.Xaml")
    }
};

var nuspecNuGetSettingsAndroid = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/Android: Avalonia.Android
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
            new NuSpecContent { Source = "Avalonia.Android.dll", Target = "lib/MonoAndroid10" },
            new NuSpecContent { Source = "Avalonia.Android.xml", Target = "lib/MonoAndroid10" },
        },
        BasePath = Directory("./src/Android/Avalonia.Android/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Android")
    }
};

var nuspecNuGetSettingsGtk = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/Gtk: Avalonia.Cairo
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
            new NuSpecContent { Source = "Avalonia.Cairo.dll", Target = "lib/net45" },
            new NuSpecContent { Source = "Avalonia.Cairo.xml", Target = "lib/net45" },
        },
        BasePath = Directory("./src/Gtk/Avalonia.Cairo/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Cairo")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src/Gtk: Avalonia.Gtk
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
            new NuSpecContent { Source = "Avalonia.Gtk.dll", Target = "lib/net45" },
            new NuSpecContent { Source = "Avalonia.Gtk.xml", Target = "lib/net45" },
        },
        BasePath = Directory("./src/Gtk/Avalonia.Gtk/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Gtk")
    },
};

var nuspecNuGetSettingsiOS = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/iOS: Avalonia.iOS
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
            new NuSpecContent { Source = "Avalonia.iOS.dll", Target = "lib/Xamarin.iOS10" },
            new NuSpecContent { Source = "Avalonia.iOS.xml", Target = "lib/Xamarin.iOS10" },
        },
        BasePath = Directory("./src/iOS/Avalonia.iOS/bin/" + dirSuffixIOS),
        OutputDirectory = nugetRoot.Combine("Avalonia.iOS")
    }
};

var nuspecNuGetSettingsSkia = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/Skia: Avalonia.Skia.Android
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
            new NuSpecContent { Source = "Avalonia.Skia.Android.dll", Target = "lib/MonoAndroid10" },
            new NuSpecContent { Source = "Avalonia.Skia.Android.xml", Target = "lib/MonoAndroid10" },
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.Android/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Skia.Android")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src/Skia: Avalonia.Skia.Desktop
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
            new NuSpecContent { Source = "Avalonia.Skia.Desktop.dll", Target = "lib/net45" },
            new NuSpecContent { Source = "Avalonia.Skia.Desktop.xml", Target = "lib/net45" },
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.Desktop/bin/" + dirSuffixSkia),
        OutputDirectory = nugetRoot.Combine("Avalonia.Skia.Desktop")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src/Skia: Avalonia.Skia.iOS
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
            new NuSpecContent { Source = "Avalonia.Skia.iOS.dll", Target = "lib/Xamarin.iOS10" },
            new NuSpecContent { Source = "Avalonia.Skia.iOS.xml", Target = "lib/Xamarin.iOS10" },
        },
        BasePath = Directory("./src/Skia/Avalonia.Skia.iOS/bin/" + dirSuffixIOS),
        OutputDirectory = nugetRoot.Combine("Avalonia.Skia.iOS")
    }
};

var nuspecNuGetSettingsWindows = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // src/Windows: Avalonia.Direct2D1
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
            new NuSpecContent { Source = "Avalonia.Direct2D1.dll", Target = "lib/net45" },
            new NuSpecContent { Source = "Avalonia.Direct2D1.xml", Target = "lib/net45" },
        },
        BasePath = Directory("./src/Windows/Avalonia.Direct2D1/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Direct2D1")
    },
    ///////////////////////////////////////////////////////////////////////////////
    // src/Windows: Avalonia.Win32
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
            new NuSpecContent { Source = "Avalonia.Win32.dll", Target = "lib/net45" },
            new NuSpecContent { Source = "Avalonia.Win32.xml", Target = "lib/net45" },
        },
        BasePath = Directory("./src/Windows/Avalonia.Win32/bin/" + dirSuffix),
        OutputDirectory = nugetRoot.Combine("Avalonia.Win32")
    },
};

var nuspecNuGetSettingsMain = new []
{
    ///////////////////////////////////////////////////////////////////////////////
    // Avalonia
    ///////////////////////////////////////////////////////////////////////////////
    new NuGetPackSettings()
    {
        Id = "Avalonia",
        Dependencies = new []
        {
            new NuSpecDependency() { Id = "Avalonia.Animation", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Base", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Controls", Version = version },
            new NuSpecDependency() { Id = "Avalonia.DesignerSupport", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Diagnostics", Version = version },
            new NuSpecDependency() { Id = "Avalonia.HtmlRenderer", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Input", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Interactivity", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Layout", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Logging.Serilog", Version = version },
            new NuSpecDependency() { Id = "Avalonia.ReactiveUI", Version = version },
            new NuSpecDependency() { Id = "Avalonia.SceneGraph", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Styling", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Themes.Default", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup", Version = version },
            new NuSpecDependency() { Id = "Avalonia.Markup.Xaml", Version = version }
        },
        OutputDirectory = nugetRoot.Combine("Avalonia")
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
            new NuSpecDependency() { Id = "Avalonia.Cairo", Version = version }
        },
        OutputDirectory = nugetRoot.Combine("Avalonia.Desktop")
    }
};

var nuspecNuGetSettings = new List<NuGetPackSettings>();

nuspecNuGetSettings.AddRange(nuspecNuGetSettingsCore);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsMarkup);

nuspecNuGetSettings.AddRange(nuspecNuGetSettingsAndroid);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsGtk);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsiOS);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsSkia);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsWindows);
nuspecNuGetSettings.AddRange(nuspecNuGetSettingsMain);

nuspecNuGetSettings.ForEach((nuspec) => SetNuGetNuspecCommonProperties(nuspec));

var nugetPackages = nuspecNuGetSettings.Select(nuspec => {
    return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
}).ToArray();

var nupkgNuGetDirs = nuspecNuGetSettings.Select(nuspec => nuspec.OutputDirectory);

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
    CleanDirectory(testResultsDir);
    CleanDirectory(nugetRoot);
    CleanDirectories(nupkgNuGetDirs);
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

    var files = isRunningOnWindows ? GetFiles(pattern) : GetFiles(pattern, ExcludeWindowsTests);

    if (platform == "x86")
    {
        XUnit2(files, new XUnit2Settings { 
            ToolPath = "./tools/xunit.runner.console/tools/xunit.console.x86.exe",
            OutputDirectory = testResultsDir,
            XmlReportV1 = true,
            NoAppDomain = true,
            Parallelism = ParallelismOption.None
        });
    }
    else
    {
        XUnit2(files, new XUnit2Settings { 
            ToolPath = "./tools/xunit.runner.console/tools/xunit.console.exe",
            OutputDirectory = testResultsDir,
            XmlReportV1 = true,
            NoAppDomain = true,
            Parallelism = ParallelismOption.None
        });
    }
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
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => isRunningOnAppVeyor)
    .Does(() =>
{
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
