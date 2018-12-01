using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Utilities;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;


/*
 Before editing this file, install support plugin for your IDE,
 running and debugging a particular target (optionally without deps) would be way easier
 ReSharper/Rider - https://plugins.jetbrains.com/plugin/10803-nuke-support
 VSCode - https://marketplace.visualstudio.com/items?itemName=nuke.support
 
 */

partial class Build : NukeBuild
{   
    BuildParameters Parameters { get; set; }
    protected override void OnBuildInitialized()
    {
        Parameters = new BuildParameters(this);
        Information("Building version {0} of Avalonia ({1}) using version {2} of Nuke.", 
            Parameters.Version,
            Parameters.Configuration,
            typeof(NukeBuild).Assembly.GetName().Version.ToString());

        if (Parameters.IsLocalBuild)
        {
            Information("Repository Name: " + Parameters.RepositoryName);
            Information("Repository Branch: " + Parameters.RepositoryBranch);
        }
        Information("Configuration: " + Parameters.Configuration);
        Information("IsLocalBuild: " + Parameters.IsLocalBuild);
        Information("IsRunningOnUnix: " + Parameters.IsRunningOnUnix);
        Information("IsRunningOnWindows: " + Parameters.IsRunningOnWindows);
        Information("IsRunningOnAppVeyor: " + Parameters.IsRunningOnAppVeyor);
        Information("IsRunnongOnAzure:" + Parameters.IsRunningOnAzure);
        Information("IsPullRequest: " + Parameters.IsPullRequest);
        Information("IsMainRepo: " + Parameters.IsMainRepo);
        Information("IsMasterBranch: " + Parameters.IsMasterBranch);
        Information("IsReleaseBranch: " + Parameters.IsReleaseBranch);
        Information("IsTagged: " + Parameters.IsTagged);
        Information("IsReleasable: " + Parameters.IsReleasable);
        Information("IsMyGetRelease: " + Parameters.IsMyGetRelease);
        Information("IsNuGetRelease: " + Parameters.IsNuGetRelease);
    }

    Target Clean => _ => _.Executes(() =>
    {
        var data = Parameters;
        DeleteDirectories(data.BuildDirs);
        EnsureCleanDirectories(data.BuildDirs);
        EnsureCleanDirectory(data.ArtifactsDir);
        EnsureCleanDirectory(data.NugetRoot);
        EnsureCleanDirectory(data.ZipRoot);
        EnsureCleanDirectory(data.TestResultsRoot);
    });


    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            var data = Parameters;
            if (data.IsRunningOnWindows)
                MSBuild(data.MSBuildSolution, c => c
                    .SetConfiguration(data.Configuration)
                    .SetVerbosity(MSBuildVerbosity.Minimal)
                    .AddProperty("PackageVersion", Parameters.Version)
                    .AddProperty("iOSRoslynPathHackRequired", "true")
                    .SetToolsVersion(MSBuildToolsVersion._15_0)
                    .AddTargets("Restore", "Build")
                );

            else
                DotNetBuild(Parameters.MSBuildSolution, c => c
                    .AddProperty("PackageVersion", Parameters.Version)
                    .SetConfiguration(Parameters.Configuration)
                );
        });
    
    void RunCoreTest(string project, bool coreOnly = false)
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
            DotNetTest(c =>
            {
                c = c
                    .SetProjectFile(project)
                    .SetConfiguration(Parameters.Configuration)
                    .SetFramework(fw)
                    .EnableNoBuild()
                    .EnableNoRestore();
                // NOTE: I can see that we could maybe add another extension method "Switch" or "If" to make this more  convenient
                if (Parameters.PublishTestResults)
                    c = c.SetLogger("trx").SetResultsDirectory(Parameters.TestResultsRoot);
                return c;
            });
        }
    }

    Target RunCoreLibsTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            
            RunCoreTest("./tests/Avalonia.Base.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Controls.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Input.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Interactivity.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Layout.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Markup.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Markup.Xaml.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Styling.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Visuals.UnitTests", false);
            RunCoreTest("./tests/Avalonia.Skia.UnitTests", false);
            RunCoreTest("./tests/Avalonia.ReactiveUI.UnitTests", false);

        });

    Target RunRenderTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj", true);
            RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj", true);
        });
    
    Target RunDesignerTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("./tests/Avalonia.DesignerSupport.Tests", false);
        });

    [PackageExecutable("JetBrains.dotMemoryUnit", "dotMemoryUnit.exe")] readonly Tool DotMemoryUnit;

    Target RunLeakTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {

            var dotMemoryUnitPath =
                ToolPathResolver.GetPackageExecutable("JetBrains.dotMemoryUnit", "dotMemoryUnit.exe");
            var xunitRunnerPath =
                ToolPathResolver.GetPackageExecutable("xunit.runner.console", "xunit.console.x86.exe");
            var args = new[]
            {
                Path.GetFullPath(xunitRunnerPath),
                "--propagate-exit-code",
                "--",
                "tests\\Avalonia.LeakTests\\bin\\Release\\net461\\Avalonia.LeakTests.dll"
            };
            var cargs = string.Join(" ", args.Select(a => '"' + a + '"'));

            var proc = Process.Start(new ProcessStartInfo(dotMemoryUnitPath, cargs)
            {
                UseShellExecute = false
            });

            if (!proc.WaitForExit(120000))
            {
                proc.Kill();
                throw new Exception("Leak tests timed out");
            }

            var leakTestsExitCode = proc.ExitCode;
            
            if (leakTestsExitCode != 0)
            {
                throw new Exception("Leak Tests failed");
            }
            

            var testAssembly = "tests\\Avalonia.LeakTests\\bin\\Release\\net461\\Avalonia.LeakTests.dll";
            DotMemoryUnit(
                $"{XunitPath.DoubleQuoteIfNeeded()} --propagate-exit-code -- {testAssembly}",
                timeout: 120_000);
        });

    Target ZipFiles => _ => _
        .After(CreateNugetPackages, Compile, RunCoreLibsTests, Package)    
        .Executes(() =>
        {
            var data = Parameters;
            Zip(data.ZipCoreArtifacts, data.BinRoot);

            Zip(data.ZipNuGetArtifacts, data.NugetRoot);

            Zip(data.ZipTargetControlCatalogDesktopDir,
                GlobFiles(data.ZipSourceControlCatalogDesktopDir, "*.dll").Concat(
                    GlobFiles(data.ZipSourceControlCatalogDesktopDir, "*.config")).Concat(
                    GlobFiles(data.ZipSourceControlCatalogDesktopDir, "*.so")).Concat(
                    GlobFiles(data.ZipSourceControlCatalogDesktopDir, "*.dylib")).Concat(
                    GlobFiles(data.ZipSourceControlCatalogDesktopDir, "*.exe")));
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(Compile)
        .After(RunTests)
        .Executes(() =>
        {
            if (Parameters.IsRunningOnWindows)

                MSBuild(Parameters.MSBuildSolution, c => c
                    .SetConfiguration(Parameters.Configuration)
                    .SetVerbosity(MSBuildVerbosity.Minimal)
                    .AddProperty("PackageVersion", Parameters.Version)
                    .AddProperty("iOSRoslynPathHackRequired", "true")
                    .SetToolsVersion(MSBuildToolsVersion._15_0)
                    .AddTargets("Restore", "Pack"));
            else
                DotNetPack(Parameters.MSBuildSolution, c =>
                    c.SetConfiguration(Parameters.Configuration)
                        .AddProperty("PackageVersion", Parameters.Version));
        });
    
    Target RunTests => _ => _
        .DependsOn(RunCoreLibsTests)
        .DependsOn(RunRenderTests)
        .DependsOn(RunDesignerTests)
        .DependsOn(RunLeakTests);
    
    Target Package => _ => _
        .DependsOn(RunTests)
        .DependsOn(CreateNugetPackages);
    
    Target CiAppVeyor => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);
    
    Target CiTravis => _ => _
        .DependsOn(RunTests);
    
    Target CiAsuzeLinux => _ => _
        .DependsOn(RunTests);
    
    Target CiAsuzeOSX => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);
    
    Target CiAsuzeWindows => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);

    
    public static int Main() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Execute<Build>(x => x.Package)
            : Execute<Build>(x => x.RunTests);

}
