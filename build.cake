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

#tool "nuget:?package=xunit.runner.console&version=2.3.0-beta5-build3769"

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
    CleanDirectory(parameters.DesignerTestsRoot);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .WithCriteria(parameters.IsRunningOnWindows)
    .Does(() =>
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
                NuGetRestore(parameters.MSBuildSolution, new NuGetRestoreSettings {
                    ToolTimeout = TimeSpan.FromMinutes(toolTimeout)
                });
        });
});

void DotNetCoreBuild()
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

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(parameters.IsRunningOnWindows)
    {
        MSBuild(parameters.MSBuildSolution, settings => {
            settings.SetConfiguration(parameters.Configuration);
            settings.SetVerbosity(Verbosity.Minimal);
            settings.WithProperty("Platform", "\"" + parameters.Platform + "\"");
            settings.WithProperty("UseRoslynPathHack", "true");
            settings.UseToolVersion(MSBuildToolVersion.VS2017);
            settings.WithProperty("Windows", "True");
            settings.SetNodeReuse(false);
        });
    }
    else
    {
        DotNetCoreBuild();
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

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .IsDependentOn("Run-Designer-Unit-Tests")
    .IsDependentOn("Run-Render-Tests")
    .WithCriteria(() => !parameters.SkipTests)
    .Does(() => {
        RunCoreTest("./tests/Avalonia.Base.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Controls.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Input.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Interactivity.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Layout.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Markup.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Markup.Xaml.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Styling.UnitTests", parameters, false);
        RunCoreTest("./tests/Avalonia.Visuals.UnitTests", parameters, false);
        if (parameters.IsRunningOnWindows)
        {
            RunCoreTest("./tests/Avalonia.Direct2D1.UnitTests", parameters, true);
        }
    });

Task("Run-Render-Tests")
    .IsDependentOn("Build")
    .WithCriteria(() => !parameters.SkipTests && parameters.IsRunningOnWindows)
    .Does(() => {
        RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj", parameters, true);
        RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj", parameters, true);
    });

Task("Run-Designer-Unit-Tests")
    .IsDependentOn("Build")
    .WithCriteria(() => !parameters.SkipTests && parameters.IsRunningOnWindows)
    .Does(() =>
{
    var toolPath = (parameters.IsPlatformAnyCPU || parameters.IsPlatformX86) ? 
        Context.Tools.Resolve("xunit.console.x86.exe") :
        Context.Tools.Resolve("xunit.console.exe");

    var xUnitSettings = new XUnit2Settings 
    { 
        ToolPath = toolPath,
        Parallelism = ParallelismOption.None,
        ShadowCopy = false,
    };

    XUnit2("./artifacts/designer-tests/Avalonia.DesignerSupport.Tests.dll", xUnitSettings);
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
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.config") + 
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.so") + 
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.dylib") + 
        GetFiles(parameters.ZipSourceControlCatalogDesktopDirs.FullPath + "/*.exe"));
});

Task("Create-NuGet-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .IsDependentOn("Inspect")
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

Task("Run-Leak-Tests")
    .WithCriteria(parameters.IsRunningOnWindows)
    .IsDependentOn("Build")
    .Does(() =>
    {
        DotNetCoreRestore("tests\\Avalonia.LeakTests\\toolproject\\tool.csproj");
        DotNetBuild("tests\\Avalonia.LeakTests\\toolproject\\tool.csproj", settings => settings.SetConfiguration("Release"));
        var report = "tests\\Avalonia.LeakTests\\bin\\Release\\report.xml";
        if(System.IO.File.Exists(report))
            System.IO.File.Delete(report);

        var toolXunitConsoleX86 = Context.Tools.Resolve("xunit.console.x86.exe").FullPath;
        var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName="tests\\Avalonia.LeakTests\\toolproject\\bin\\dotMemoryUnit.exe",
            Arguments="-targetExecutable=\"" + toolXunitConsoleX86 + "\" -returnTargetExitCode  -- tests\\Avalonia.LeakTests\\bin\\Release\\Avalonia.LeakTests.dll -xml tests\\Avalonia.LeakTests\\bin\\Release\\report.xml ",
            UseShellExecute = false,
        });
        var st = System.Diagnostics.Stopwatch.StartNew();
        while(!proc.HasExited && !System.IO.File.Exists(report))
        {
            if(st.Elapsed.TotalSeconds>60)
            {
                Error("Timed out, probably a bug in dotMemoryUnit");
                proc.Kill();
                throw new Exception("dotMemory issue");
            }
            proc.WaitForExit(100);
        }
        try{
            proc.Kill();
        }catch{}
        var doc =  System.Xml.Linq.XDocument.Load(report);
        if(doc.Root.Descendants("assembly").Any(x=>x.Attribute("failed").Value.ToString() != "0"))
        {
            throw new Exception("Tests failed");
        }

    });

Task("Inspect")
    .WithCriteria(parameters.IsRunningOnWindows)
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
    {
        var badIssues = new []{"PossibleNullReferenceException"};
        var whitelist = new []{"tests", "src\\android", "src\\ios",
            "src\\windows\\avalonia.designer", "src\\avalonia.htmlrenderer\\external",
            "src\\markup\\avalonia.markup.xaml\\portablexaml\\portable.xaml.github"};
        Information("Running code inspections");
        
        StartProcess(Context.Tools.Resolve("inspectcode.exe"),
            new ProcessSettings{ Arguments = "--output=artifacts\\inspectcode.xml --profile=Avalonia.sln.DotSettings Avalonia.sln" });
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
// TARGETS
///////////////////////////////////////////////////////////////////////////////

Task("Package")
  .IsDependentOn("Create-NuGet-Packages");

Task("Default").Does(() =>
{
    if(parameters.IsRunningOnWindows)
        RunTarget("Package");
    else
        RunTarget("Run-Unit-Tests");
});
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
