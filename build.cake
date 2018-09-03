///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=NuGet.CommandLine&version=4.7.1"
#tool "nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2018.2.3"
#tool "nuget:?package=xunit.runner.console&version=2.3.1"
#tool "nuget:?package=JetBrains.dotMemoryUnit&version=3.0.20171219.105559"

///////////////////////////////////////////////////////////////////////////////
// USINGS
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

///////////////////////////////////////////////////////////////////////////////
// SCRIPTS
///////////////////////////////////////////////////////////////////////////////

#load "./parameters.cake"

///////////////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////////////

Setup<Parameters>(context =>
{
    var parameters = new Parameters(context);

    Information("Building version {0} of Avalonia ({1}) using version {2} of Cake.", 
        parameters.Version,
        parameters.Configuration,
        typeof(ICakeContext).Assembly.GetName().Version.ToString());

    if (parameters.IsRunningOnAppVeyor)
    {
        Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
        Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
    }
    Information("Target:" + context.TargetTask.Name);
    Information("Configuration: " + parameters.Configuration);
    Information("IsLocalBuild: " + parameters.IsLocalBuild);
    Information("IsRunningOnUnix: " + parameters.IsRunningOnUnix);
    Information("IsRunningOnWindows: " + parameters.IsRunningOnWindows);
    Information("IsRunningOnAppVeyor: " + parameters.IsRunningOnAppVeyor);
    Information("IsRunnongOnAzure:" + parameters.IsRunningOnAzure);
    Information("IsPullRequest: " + parameters.IsPullRequest);
    Information("IsMainRepo: " + parameters.IsMainRepo);
    Information("IsMasterBranch: " + parameters.IsMasterBranch);
    Information("IsReleaseBranch: " + parameters.IsReleaseBranch);
    Information("IsTagged: " + parameters.IsTagged);
    Information("IsReleasable: " + parameters.IsReleasable);
    Information("IsMyGetRelease: " + parameters.IsMyGetRelease);
    Information("IsNuGetRelease: " + parameters.IsNuGetRelease);

    return parameters;
});

///////////////////////////////////////////////////////////////////////////////
// TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Teardown<Parameters>((context, buildContext) =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean-Impl")
    .Does<Parameters>(data =>
{
    CleanDirectories(data.BuildDirs);
    CleanDirectory(data.ArtifactsDir);
    CleanDirectory(data.NugetRoot);
    CleanDirectory(data.ZipRoot);
    CleanDirectory(data.TestResultsRoot);
});

void DotNetCoreBuild(Parameters parameters)
{
    var settings = new DotNetCoreBuildSettings 
    {
        Configuration = parameters.Configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings
        {
            Properties =
            {
                { "PackageVersion", new [] { parameters.Version } }
            }
        }
    };

    DotNetCoreBuild(parameters.MSBuildSolution, settings);
}

Task("Build-Impl")
    .Does<Parameters>(data =>
{
    if(data.IsRunningOnWindows)
    {
        MSBuild(data.MSBuildSolution, settings => {
            settings.SetConfiguration(data.Configuration);
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("iOSRoslynPathHackRequired", "true");
            settings.WithProperty("PackageVersion", data.Version);
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithRestore();
        });
    }
    else
    {
        DotNetCoreBuild(data);
    }
});

void RunCoreTest(string project, Parameters parameters, bool coreOnly = false)
{
    if(!project.EndsWith(".csproj"))
        project = System.IO.Path.Combine(project, System.IO.Path.GetFileName(project)+".csproj");
    Information("Running tests from " + project);
    var frameworks = new List<string>(){"netcoreapp2.0"};
    foreach(var fw in frameworks)
    {
        if(!fw.StartsWith("netcoreapp") && coreOnly)
            continue;
        Information("Running for " + fw);
        
        var settings = new DotNetCoreTestSettings {
            Configuration = parameters.Configuration,
            Framework = fw,
            NoBuild = true,
            NoRestore = true
        };

        if (parameters.PublishTestResults)
        {
            settings.Logger = "trx";
            settings.ResultsDirectory = parameters.TestResultsRoot;
        }

        DotNetCoreTest(project, settings);
    }
}

Task("Run-Unit-Tests-Impl")
    .WithCriteria<Parameters>((context, data) => !data.SkipTests)
    .Does<Parameters>(data =>
{
    RunCoreTest("./tests/Avalonia.Base.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Controls.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Input.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Interactivity.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Layout.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Markup.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Markup.Xaml.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Styling.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Visuals.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.Skia.UnitTests", data, false);
    RunCoreTest("./tests/Avalonia.ReactiveUI.UnitTests", data, false);
    if (data.IsRunningOnWindows)
    {
        //RunCoreTest("./tests/Avalonia.Direct2D1.UnitTests", data, false);
    }
});

Task("Run-Designer-Tests-Impl")
    .WithCriteria<Parameters>((context, data) => !data.SkipTests)
    .Does<Parameters>(data =>
{
    RunCoreTest("./tests/Avalonia.DesignerSupport.Tests", data, false);
});

Task("Run-Render-Tests-Impl")
    .WithCriteria<Parameters>((context, data) => !data.SkipTests)
    .WithCriteria<Parameters>((context, data) => data.IsRunningOnWindows)
    .Does<Parameters>(data =>
{
    RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj", data, true);
    RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj", data, true);
});

Task("Run-Leak-Tests-Impl")
    .WithCriteria<Parameters>((context, data) => !data.SkipTests)
    .WithCriteria<Parameters>((context, data) => data.IsRunningOnWindows)
    .Does(() =>
{
    var dotMemoryUnit = Context.Tools.Resolve("dotMemoryUnit.exe");
    var leakTestsExitCode = StartProcess(dotMemoryUnit, new ProcessSettings
    {
        Arguments = new ProcessArgumentBuilder()
            .Append(Context.Tools.Resolve("xunit.console.x86.exe").FullPath)
            .Append("--propagate-exit-code")
            .Append("--")
            .Append("tests\\Avalonia.LeakTests\\bin\\Release\\net461\\Avalonia.LeakTests.dll"),
        Timeout = 120000
    });

    if (leakTestsExitCode != 0)
    {
        throw new Exception("Leak Tests failed");
    }
});

Task("Zip-Files-Impl")
    .Does<Parameters>(data =>
{
    Zip(data.BinRoot, data.ZipCoreArtifacts);

    Zip(data.NugetRoot, data.ZipNuGetArtifacts);

    Zip(data.ZipSourceControlCatalogDesktopDirs, 
        data.ZipTargetControlCatalogDesktopDirs, 
        GetFiles(data.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dll") + 
        GetFiles(data.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.config") + 
        GetFiles(data.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.so") + 
        GetFiles(data.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dylib") + 
        GetFiles(data.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.exe"));
});

void DotNetCorePack(Parameters parameters)
{
    var settings = new DotNetCorePackSettings 
    {
        Configuration = parameters.Configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings
        {
            Properties =
            {
                { "PackageVersion", new [] { parameters.Version } }
            }
        }
    };

    DotNetCorePack(parameters.MSBuildSolution, settings);
}

Task("Create-NuGet-Packages-Impl")
    .Does<Parameters>(data =>
{
    if(data.IsRunningOnWindows)
    {
        MSBuild(data.MSBuildSolution, settings => {
            settings.SetConfiguration(data.Configuration);
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("iOSRoslynPathHackRequired", "true");
            settings.WithProperty("PackageVersion", data.Version);
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithRestore();
            settings.WithTarget("Pack");
        });
    }
    else
    {
        DotNetCorePack(data);
    }
});

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Build")
    .IsDependentOn("Clean-Impl")
    .IsDependentOn("Build-Impl");

Task("Run-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests-Impl")
    //.IsDependentOn("Run-Render-Tests-Impl")
    .IsDependentOn("Run-Designer-Tests-Impl")
    .IsDependentOn("Run-Leak-Tests-Impl");

Task("Package")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Create-NuGet-Packages-Impl");

Task("AppVeyor")
  .IsDependentOn("Package")
  .IsDependentOn("Zip-Files-Impl");

Task("Travis")
  .IsDependentOn("Run-Tests");

Task("Azure-Linux")
  .IsDependentOn("Run-Tests");

Task("Azure-OSX")
  .IsDependentOn("Package")
  .IsDependentOn("Zip-Files-Impl");

Task("Azure-Windows")
  .IsDependentOn("Package")
  .IsDependentOn("Zip-Files-Impl");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

var target = Context.Argument("target", "Default");

if (target == "Default")
{
    target = Context.IsRunningOnWindows() ? "Package" : "Run-Tests";
}

RunTarget(target);
