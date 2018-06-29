///////////////////////////////////////////////////////////////////////////////
// ADDINS
///////////////////////////////////////////////////////////////////////////////

#addin "nuget:?package=Polly&version=5.3.1"
#addin "nuget:?package=NuGet.Core&version=2.14.0"
#tool "nuget:?package=NuGet.CommandLine&version=4.3.0"
#tool "nuget:?package=JetBrains.ReSharper.CommandLineTools&version=2017.1.20170613.162720"
///////////////////////////////////////////////////////////////////////////////
// TOOLS
///////////////////////////////////////////////////////////////////////////////

#tool "nuget:?package=xunit.runner.console&version=2.3.1"
#tool "nuget:?package=JetBrains.dotMemoryUnit&version=3.0.20171219.105559"

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

    Information("Building version {0} of Avalonia ({1}, {2}) using version {3} of Cake.", 
        parameters.Version,
        parameters.Platform,
        parameters.Configuration,
        typeof(ICakeContext).Assembly.GetName().Version.ToString());

    if (parameters.IsRunningOnAppVeyor)
    {
        Information("Repository Name: " + BuildSystem.AppVeyor.Environment.Repository.Name);
        Information("Repository Branch: " + BuildSystem.AppVeyor.Environment.Repository.Branch);
    }
    Information("Target:" + context.TargetTask.Name);
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
// TASKS IMPLEMENTATIONS
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

Task("Restore-NuGet-Packages-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsRunningOnWindows)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.Platform != "NetCoreOnly")
    .Does<AvaloniaBuildData>(data =>
{
    var maxRetryCount = 5;
    var toolTimeout = 2d;
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
                NuGetRestore(data.Parameters.MSBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
        });
});

void DotNetCoreBuild(Parameters parameters)
{
    var settings = new DotNetCoreBuildSettings 
    {
        Configuration = parameters.Configuration,
        MSBuildSettings = new DotNetCoreMSBuildSettings(),
    };

    settings.MSBuildSettings.SetConfiguration(parameters.Configuration);
    settings.MSBuildSettings.WithProperty("Platform", "\"" + parameters.Platform + "\"");

    DotNetCoreBuild(parameters.MSBuildSolution, settings);
}

Task("Build-Impl")
    .Does<AvaloniaBuildData>(data =>
{
    if(data.Parameters.IsRunningOnWindows && data.Parameters.Platform != "NetCoreOnly")
    {
        MSBuild(data.Parameters.MSBuildSolution, settings => {
            settings.SetConfiguration(data.Parameters.Configuration);
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("Platform", "\"" + data.Parameters.Platform + "\"");
            settings.WithProperty("UseRoslynPathHack", "true");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithProperty("Windows", "True");
            settings.SetNodeReuse(false);
            settings.SetMaxCpuCount(0);
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
    .Does<AvaloniaBuildData>(data => {
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
        if (data.Parameters.IsRunningOnWindows && data.Parameters.Platform != "NetCoreOnly")
        {
            RunCoreTest("./tests/Avalonia.Direct2D1.UnitTests", data.Parameters, true);
        }
    });

Task("Run-Designer-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests)
    .Does<AvaloniaBuildData>(data => {
        RunCoreTest("./tests/Avalonia.DesignerSupport.Tests", data.Parameters, false);
    });

Task("Run-Render-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests && data.Parameters.IsRunningOnWindows)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.Platform != "NetCoreOnly")
    .Does<AvaloniaBuildData>(data => {
        RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj", data.Parameters, true);
        RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj", data.Parameters, true);
    });

Task("Run-Leak-Tests-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.SkipTests && data.Parameters.IsRunningOnWindows)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.Platform != "NetCoreOnly")
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

Task("Publish-MyGet-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.IsLocalBuild)
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.IsPullRequest)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsMainRepo)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsMasterBranch)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsMyGetRelease)
    .Does<AvaloniaBuildData>(data =>
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

    foreach(var nupkg in data.Packages.NugetPackages)
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

Task("Publish-NuGet-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.IsLocalBuild)
    .WithCriteria<AvaloniaBuildData>((context, data) => !data.Parameters.IsPullRequest)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsMainRepo)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsNuGetRelease)
    .Does<AvaloniaBuildData>(data =>
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

    foreach(var nupkg in data.Packages.NugetPackages)
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

Task("Inspect-Impl")
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.IsRunningOnWindows)
    .WithCriteria<AvaloniaBuildData>((context, data) => data.Parameters.Platform != "NetCoreOnly")
    .Does(() =>
    {
        var badIssues = new []{"PossibleNullReferenceException"};
        var whitelist = new []{"tests", "src\\android", "src\\ios",
            "src\\markup\\avalonia.markup.xaml\\portablexaml\\portable.xaml.github"};
        Information("Running code inspections");
        
        var exitCode = StartProcess(Context.Tools.Resolve("inspectcode.exe"),
            new ProcessSettings
            {
                Arguments = "--output=artifacts\\inspectcode.xml --profile=Avalonia.sln.DotSettings Avalonia.sln",
                RedirectStandardOutput = true
            });

        Information("Analyzing report");
        var doc = XDocument.Parse(System.IO.File.ReadAllText("artifacts\\inspectcode.xml"));
        var failBuild = false;
        foreach(var xml in doc.Descendants("Issue"))
        {
            var typeId = xml.Attribute("TypeId").Value.ToString();
            if(badIssues.Contains(typeId))
            {
                var file = xml.Attribute("File").Value.ToString().ToLower();
                if(whitelist.Any(wh => file.StartsWith(wh)))
                    continue;
                var line = xml.Attribute("Line").Value.ToString();
                Error(typeId + " - " + file + " on line " + line);
                failBuild = true;
            }
        }
        if(failBuild)
            throw new Exception("Issues found");
    });

///////////////////////////////////////////////////////////////////////////////
// TASKS DEPENDENCY TREE
///////////////////////////////////////////////////////////////////////////////

Task("Clean")
    .IsDependentOn("Clean-Impl");

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore-NuGet-Packages-Impl");

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Build-Impl");

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Unit-Tests-Impl");

Task("Run-Designer-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Designer-Tests-Impl");

Task("Run-Render-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Render-Tests-Impl");

Task("Run-Leak-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Leak-Tests-Impl");

Task("Run-Tests")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Run-Render-Tests")
    .IsDependentOn("Run-Designer-Tests")
    .IsDependentOn("Run-Leak-Tests");

Task("Copy-Files")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Copy-Files-Impl");

Task("Zip-Files")
    .IsDependentOn("Copy-Files")
    .IsDependentOn("Zip-Files-Impl");

Task("Create-NuGet-Packages")
    .IsDependentOn("Run-Tests")
    .IsDependentOn("Inspect")
    .IsDependentOn("Create-NuGet-Packages-Impl");

Task("Publish-MyGet")
    .IsDependentOn("Create-NuGet-Packages")
    .IsDependentOn("Publish-MyGet-Impl");

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Packages")
    .IsDependentOn("Publish-NuGet-Impl");

Task("Inspect")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Inspect-Impl");

///////////////////////////////////////////////////////////////////////////////
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
  .IsDependentOn("Create-NuGet-Packages");

Task("AppVeyor")
  .IsDependentOn("Zip-Files")
  .IsDependentOn("Publish-MyGet")
  .IsDependentOn("Publish-NuGet");

Task("Travis")
  .IsDependentOn("Run-Tests");

///////////////////////////////////////////////////////////////////////////////
// EXECUTE
///////////////////////////////////////////////////////////////////////////////

var target = Context.Argument("target", "Default");

if (target == "Default")
{
    target = Context.IsRunningOnWindows() ? "Package" : "Run-Tests";
}

RunTarget(target);
