///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=4.2.0"
#addin "nuget:?package=NuGet.Core&version=2.12.0"
#tool "nuget:https://dotnet.myget.org/F/nuget-build/?package=NuGet.CommandLine&version=4.3.0-beta1-2361&prerelease"

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
// SCRIPTS
///////////////////////////////////////////////////////////////////////////////

#load "./parameters.cake"
#load "./packages.cake"

//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////

Parameters parameters = new Parameters(Context);
Packages packages = new Packages(Context, parameters);

///////////////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////////////

Setup(context =>
{
    Information("Building version {0} of Avalonia ({1}, {2}, {3}) using version {4} of Cake.", 
        parameters.Version,
        parameters.Platform,
        parameters.Configuration,
        parameters.Target,
        typeof(ICakeContext).Assembly.GetName().Version.ToString());

    if (parameters.IsRunningOnAppVeyor)
    {
        Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
        Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
    }

    Information("Target: " + parameters.Target);
    Information("Platform: " + parameters.Platform);
    Information("Configuration: " + parameters.Configuration);
    Information("IsLocalBuild: " + parameters.IsLocalBuild);
    Information("IsRunningOnUnix: " + parameters.IsRunningOnUnix);
    Information("IsRunningOnWindows: " + parameters.IsRunningOnWindows);
    Information("IsRunningOnAppVeyor: " + parameters.IsRunningOnAppVeyor);
    Information("IsPullRequest: " + parameters.IsPullRequest);
    Information("IsMainRepo: " + parameters.IsMainRepo);
    Information("IsMasterBranch: " + parameters.IsMasterBranch);
    Information("IsTagged: " + parameters.IsTagged);
    Information("IsReleasable: " + parameters.IsReleasable);
    Information("IsMyGetRelease: " + parameters.IsMyGetRelease);
    Information("IsNuGetRelease: " + parameters.IsNuGetRelease);
});

///////////////////////////////////////////////////////////////////////////////
// TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Teardown(context =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectories(parameters.BuildDirs);
    CleanDirectory(parameters.ArtifactsDir);
    CleanDirectory(parameters.NugetRoot);
    CleanDirectory(parameters.ZipRoot);
    CleanDirectory(parameters.BinRoot);
    CleanDirectory(parameters.TestsRoot);
});


Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .WithCriteria(parameters.IsRunningOnWindows)
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
                NuGetRestore(parameters.MSBuildSolution, new NuGetRestoreSettings {
                    ToolPath = "./tools/NuGet.CommandLine/tools/NuGet.exe",
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
        });
});

void DotNetCoreBuild()
{
}

Task("DotNetCoreBuild")
    .IsDependentOn("Clean")
    .Does(() => DotNetCoreBuild());

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(parameters.IsRunningOnWindows)
    {
        MSBuild(parameters.MSBuildSolution, settings => {
            settings.SetConfiguration(parameters.Configuration);
            settings.WithProperty("Platform", "\"" + parameters.Platform + "\"");
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("Windows", "True");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.SetNodeReuse(false);
        });
    }
    else
    {
        DotNetCoreBuild();
    }
});

void RunDotNetCoreTest()
{
}

Task("Run-Net-Core-Unit-Tests")
    .IsDependentOn("Clean")
    .Does(() => RunDotNetCoreTest());

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .WithCriteria(() => !parameters.SkipTests)
    .Does(() =>
{
    if(parameters.IsRunningOnWindows)
    {
        RunDotNetCoreTest();
    }

    var unitTests = GetDirectories("./tests/Avalonia.*.UnitTests")
        .Select(dir => System.IO.Path.GetFileName(dir.FullPath))
        .Where(name => parameters.IsRunningOnWindows ? true : !(name.IndexOf("Direct2D", StringComparison.OrdinalIgnoreCase) >= 0))
        .Select(name => MakeAbsolute(File("./tests/" + name + "/bin/" + parameters.DirSuffix + "/" + name + ".dll")))
        .ToList();

    if (parameters.IsRunningOnWindows)
    {
        var leakTests = GetFiles("./tests/Avalonia.LeakTests/bin/" + parameters.DirSuffix + "/*.LeakTests.dll");

        unitTests.AddRange(leakTests);
    }

    var toolPath = (parameters.IsPlatformAnyCPU || parameters.IsPlatformX86) ? 
        "./tools/xunit.runner.console/tools/xunit.console.x86.exe" :
        "./tools/xunit.runner.console/tools/xunit.console.exe";

    var xUnitSettings = new XUnit2Settings 
    { 
        ToolPath = toolPath,
        Parallelism = ParallelismOption.None,
        ShadowCopy = false
    };

    xUnitSettings.NoAppDomain = !parameters.IsRunningOnWindows;

    var openCoverOutput = parameters.ArtifactsDir.GetFilePath(new FilePath("./coverage.xml"));
    var openCoverSettings = new OpenCoverSettings()
        .WithFilter("+[Avalonia.*]* -[*Test*]* -[ControlCatalog*]*")
        .WithFilter("-[Avalonia.*]OmniXaml.* -[Avalonia.*]Glass.*")
        .WithFilter("-[Avalonia.HtmlRenderer]TheArtOfDev.HtmlRenderer.* +[Avalonia.HtmlRenderer]TheArtOfDev.HtmlRenderer.Avalonia.* -[Avalonia.ReactiveUI]*");
    
    openCoverSettings.ReturnTargetCodeOffset = 0;

    foreach(var test in unitTests.Where(testFile => FileExists(testFile)))
    {
        CopyDirectory(test.GetDirectory(), parameters.TestsRoot);
    }
    
    CopyFile(System.IO.Path.Combine(packages.NugetPackagesDir, "SkiaSharp", packages.SkiaSharpVersion,
        "runtimes", "win7-x86", "native", "libSkiaSharp.dll"),
        System.IO.Path.Combine(parameters.TestsRoot.ToString(), "libSkiaSharp.dll"));
    
    var testsInDirectoryToRun = new List<FilePath>();
    if(parameters.IsRunningOnWindows)
    {
        testsInDirectoryToRun.AddRange(GetFiles("./artifacts/tests/*Tests.dll"));
    }
    else
    {
        testsInDirectoryToRun.AddRange(GetFiles("./artifacts/tests/*.UnitTests.dll"));
    }

    if(parameters.IsRunningOnWindows)
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
    CopyFiles(packages.BinFiles, parameters.BinRoot);
});

Task("Zip-Files")
    .IsDependentOn("Copy-Files")
    .Does(() =>
{
    Zip(parameters.BinRoot, parameters.ZipCoreArtifacts);

    Zip(parameters.ZipSourceControlCatalogDesktopDirs, 
        parameters.ZipTargetControlCatalogDesktopDirs, 
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dll") + 
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.exe"));
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    foreach(var nuspec in packages.NuspecNuGetSettings)
    {
        NuGetPack(nuspec);
    }
});

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Packages")
    .WithCriteria(() => !parameters.IsLocalBuild)
    .WithCriteria(() => !parameters.IsPullRequest)
    .WithCriteria(() => parameters.IsMainRepo)
    .WithCriteria(() => parameters.IsMasterBranch)
    .WithCriteria(() => parameters.IsMyGetRelease)
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

    foreach(var nupkg in packages.NugetPackages)
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
    .WithCriteria(() => !parameters.IsLocalBuild)
    .WithCriteria(() => !parameters.IsPullRequest)
    .WithCriteria(() => parameters.IsMainRepo)
    .WithCriteria(() => parameters.IsMasterBranch)
    .WithCriteria(() => parameters.IsNuGetRelease)
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

    foreach(var nupkg in packages.NugetPackages)
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

RunTarget(parameters.Target);
