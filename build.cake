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
#load "./packages.cake"

//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////

class AvaloniaBuildData
{
    public AvaloniaBuildData(Parameters parameters, Packages packages)
    {
        Parameters = parameters;
        Packages = packages;
    }

    public Parameters Parameters { get; }
    public Packages Packages { get; }
}

///////////////////////////////////////////////////////////////////////////////
// SETUP
///////////////////////////////////////////////////////////////////////////////

Setup<AvaloniaBuildData>(context =>
{
    var parameters = new Parameters(context);
    var buildContext = new AvaloniaBuildData(parameters, new Packages(context, parameters));

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
    Information("IsTagged: " + parameters.IsTagged);
    Information("IsReleasable: " + parameters.IsReleasable);
    Information("IsMyGetRelease: " + parameters.IsMyGetRelease);
    Information("IsNuGetRelease: " + parameters.IsNuGetRelease);

    return buildContext;
});

///////////////////////////////////////////////////////////////////////////////
// TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Teardown<AvaloniaBuildData>((context, buildContext) =>
{
    Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("Clean-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    CleanDirectories(data.Parameters.BuildDirs);
    CleanDirectory(data.Parameters.ArtifactsDir);
    CleanDirectory(data.Parameters.NugetRoot);
    CleanDirectory(data.Parameters.ZipRoot);
    CleanDirectory(data.Parameters.BinRoot);
});

void DotNetCoreBuild(Parameters parameters)
{
    var settings = new DotNetCoreBuildSettings 
    {
        Configuration = parameters.Configuration,
    };

    DotNetCoreBuild(parameters.MSBuildSolution, settings);
}

Task("Build-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    if(data.Parameters.IsRunningOnWindows)
    {
        MSBuild(data.Parameters.MSBuildSolution, settings => {
            settings.SetConfiguration(data.Parameters.Configuration);
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("iOSRoslynPathHackRequired", "true");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithRestore();
        });
    }
    else
    {
        DotNetCoreBuild(data.Parameters);
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
        
        DotNetCoreTest(project,
            new DotNetCoreTestSettings {
                Configuration = parameters.Configuration,
                Framework = fw,
                NoBuild = true,
                NoRestore = true
            });
    }
}

Task("Run-Unit-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests)
    .Does<AvaloniaBuildData>(data =>
{
    RunCoreTest("./tests/Avalonia.Base.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Controls.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Input.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Interactivity.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Layout.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Markup.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Markup.Xaml.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Styling.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Visuals.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.Skia.UnitTests", data.Parameters, false);
    RunCoreTest("./tests/Avalonia.ReactiveUI.UnitTests", data.Parameters, false);
    if (data.Parameters.IsRunningOnWindows)
    {
        RunCoreTest("./tests/Avalonia.Direct2D1.UnitTests", data.Parameters, false);
    }
});

Task("Run-Designer-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests)
    .Does<AvaloniaBuildData>(data =>
{
    RunCoreTest("./tests/Avalonia.DesignerSupport.Tests", data.Parameters, false);
});

Task("Run-Render-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsRunningOnWindows)
    .Does<AvaloniaBuildData>(data =>
{
    RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj", data.Parameters, true);
    RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj", data.Parameters, true);
});

Task("Run-Leak-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsRunningOnWindows)
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

Task("Copy-Files-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    CopyFiles(data.Packages.BinFiles, data.Parameters.BinRoot);
});

Task("Zip-Files-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    Zip(data.Parameters.BinRoot, data.Parameters.ZipCoreArtifacts);

    Zip(data.Parameters.NugetRoot, data.Parameters.ZipNuGetArtifacts);

    Zip(data.Parameters.ZipSourceControlCatalogDesktopDirs, 
        data.Parameters.ZipTargetControlCatalogDesktopDirs, 
        GetFiles(data.Parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dll") + 
        GetFiles(data.Parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.config") + 
        GetFiles(data.Parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.so") + 
        GetFiles(data.Parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dylib") + 
        GetFiles(data.Parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.exe"));
});

Task("Create-NuGet-Packages-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    foreach(var nuspec in data.Packages.NuspecNuGetSettings)
    {
        NuGetPack(nuspec);
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
    .IsDependentOn("Run-Render-Tests-Impl")
    .IsDependentOn("Run-Designer-Tests-Impl")
    .IsDependentOn("Run-Leak-Tests-Impl");

Task("Package")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Create-NuGet-Packages-Impl");

Task("AppVeyor")
  .IsDependentOn("Package")
  .IsDependentOn("Copy-Files-Impl")
  .IsDependentOn("Zip-Files-Impl");

Task("Travis")
  .IsDependentOn("Run-Tests");

Task("Azure-Linux")
  .IsDependentOn("Run-Tests");

Task("Azure-OSX")
  .IsDependentOn("Run-Tests")
  .IsDependentOn("Copy-Files-Impl")
  .IsDependentOn("Zip-Files-Impl");

Task("Azure-Windows")
  .IsDependentOn("Package")
  .IsDependentOn("Copy-Files-Impl")
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
