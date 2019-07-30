using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
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
using static Nuke.Common.Tools.VSWhere.VSWhereTasks;

/*
 Before editing this file, install support plugin for your IDE,
 running and debugging a particular target (optionally without deps) would be way easier
 ReSharper/Rider - https://plugins.jetbrains.com/plugin/10803-nuke-support
 VSCode - https://marketplace.visualstudio.com/items?itemName=nuke.support
 
 */

partial class Build : NukeBuild
{
    static Lazy<string> MsBuildExe = new Lazy<string>(() =>
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return null;

        var msBuildDirectory = VSWhere("-latest -nologo -property installationPath -format value -prerelease").FirstOrDefault().Text;

        if (!string.IsNullOrWhiteSpace(msBuildDirectory))
        {
            string msBuildExe = Path.Combine(msBuildDirectory, @"MSBuild\Current\Bin\MSBuild.exe");
            if (!System.IO.File.Exists(msBuildExe))
                msBuildExe = Path.Combine(msBuildDirectory, @"MSBuild\15.0\Bin\MSBuild.exe");

            return msBuildExe;
        }

        return null;
    }, false);

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
        Information("IsRunningOnAzure:" + Parameters.IsRunningOnAzure);
        Information("IsPullRequest: " + Parameters.IsPullRequest);
        Information("IsMainRepo: " + Parameters.IsMainRepo);
        Information("IsMasterBranch: " + Parameters.IsMasterBranch);
        Information("IsReleaseBranch: " + Parameters.IsReleaseBranch);
        Information("IsReleasable: " + Parameters.IsReleasable);
        Information("IsMyGetRelease: " + Parameters.IsMyGetRelease);
        Information("IsNuGetRelease: " + Parameters.IsNuGetRelease);

        void ExecWait(string preamble, string command, string args)
        {
            Console.WriteLine(preamble);
            Process.Start(new ProcessStartInfo(command, args) {UseShellExecute = false}).WaitForExit();
        }
        ExecWait("dotnet version:", "dotnet", "--version");
        if (Parameters.IsRunningOnUnix)
            ExecWait("Mono version:", "mono", "--version");


    }

    IReadOnlyCollection<Output> MsBuildCommon(
        string projectFile,
        Configure<MSBuildSettings> configurator = null)
    {
        return MSBuild(projectFile, c =>
        {
            // This is required for VS2019 image on Azure Pipelines
            if (Parameters.IsRunningOnWindows && Parameters.IsRunningOnAzure)
            {
                var javaSdk = Environment.GetEnvironmentVariable("JAVA_HOME_8_X64");
                if (javaSdk != null)
                    c = c.AddProperty("JavaSdkDirectory", javaSdk);
            }

            c = c.AddProperty("PackageVersion", Parameters.Version)
                .AddProperty("iOSRoslynPathHackRequired", "true")
                .SetToolPath(MsBuildExe.Value)
                .SetConfiguration(Parameters.Configuration)
                .SetVerbosity(MSBuildVerbosity.Minimal);
            c = configurator?.Invoke(c) ?? c;
            return c;
        });
    }
    Target Clean => _ => _.Executes(() =>
    {
        DeleteDirectories(Parameters.BuildDirs);
        EnsureCleanDirectories(Parameters.BuildDirs);
        EnsureCleanDirectory(Parameters.ArtifactsDir);
        EnsureCleanDirectory(Parameters.NugetIntermediateRoot);
        EnsureCleanDirectory(Parameters.NugetRoot);
        EnsureCleanDirectory(Parameters.ZipRoot);
        EnsureCleanDirectory(Parameters.TestResultsRoot);
    });

    Target Compile => _ => _
        .DependsOn(Clean)
        .Executes(() =>
        {
            if (Parameters.IsRunningOnWindows)
                MsBuildCommon(Parameters.MSBuildSolution, c => c
                    .SetArgumentConfigurator(a => a.Add("/r"))
                    .AddTargets("Build")
                );

            else
                DotNetBuild(Parameters.MSBuildSolution, c => c
                    .AddProperty("PackageVersion", Parameters.Version)
                    .SetConfiguration(Parameters.Configuration)
                );
        });
    
    void RunCoreTest(string project)
    {
        if(!project.EndsWith(".csproj"))
            project = System.IO.Path.Combine(project, System.IO.Path.GetFileName(project)+".csproj");
        Information("Running tests from " + project);
        XDocument xdoc;
        using (var s = File.OpenRead(project))
            xdoc = XDocument.Load(s);

        List<string> frameworks = null;
        var targets = xdoc.Root.Descendants("TargetFrameworks").FirstOrDefault();
        if (targets != null)
            frameworks = targets.Value.Split(';').Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        else 
            frameworks = new List<string> {xdoc.Root.Descendants("TargetFramework").First().Value};
        
        foreach(var fw in frameworks)
        {
            if (fw.StartsWith("net4")
                && RuntimeInformation.IsOSPlatform(OSPlatform.Linux) 
                && Environment.GetEnvironmentVariable("FORCE_LINUX_TESTS") != "1")
            {
                Information($"Skipping {fw} tests on Linux - https://github.com/mono/mono/issues/13969");
                continue;
            }

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
            RunCoreTest("./tests/Avalonia.Animation.UnitTests");
            RunCoreTest("./tests/Avalonia.Base.UnitTests");
            RunCoreTest("./tests/Avalonia.Controls.UnitTests");
            RunCoreTest("./tests/Avalonia.Input.UnitTests");
            RunCoreTest("./tests/Avalonia.Interactivity.UnitTests");
            RunCoreTest("./tests/Avalonia.Layout.UnitTests");
            RunCoreTest("./tests/Avalonia.Markup.UnitTests");
            RunCoreTest("./tests/Avalonia.Markup.Xaml.UnitTests");
            RunCoreTest("./tests/Avalonia.Styling.UnitTests");
            RunCoreTest("./tests/Avalonia.Visuals.UnitTests");
            RunCoreTest("./tests/Avalonia.Skia.UnitTests");
            RunCoreTest("./tests/Avalonia.ReactiveUI.UnitTests");
        });

    Target RunRenderTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("./tests/Avalonia.Skia.RenderTests/Avalonia.Skia.RenderTests.csproj");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                RunCoreTest("./tests/Avalonia.Direct2D1.RenderTests/Avalonia.Direct2D1.RenderTests.csproj");
        });
    
    Target RunDesignerTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("./tests/Avalonia.DesignerSupport.Tests");
        });

    [PackageExecutable("JetBrains.dotMemoryUnit", "dotMemoryUnit.exe")] readonly Tool DotMemoryUnit;

    Target RunLeakTests => _ => _
        .OnlyWhen(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
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

    Target CreateIntermediateNugetPackages => _ => _
        .DependsOn(Compile)
        .After(RunTests)
        .Executes(() =>
        {
            if (Parameters.IsRunningOnWindows)

                MsBuildCommon(Parameters.MSBuildSolution, c => c
                    .AddTargets("Pack"));
            else
                DotNetPack(Parameters.MSBuildSolution, c =>
                    c.SetConfiguration(Parameters.Configuration)
                        .AddProperty("PackageVersion", Parameters.Version));
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(CreateIntermediateNugetPackages)
        .Executes(() =>
        {
            var config = Numerge.MergeConfiguration.LoadFile(RootDirectory / "nukebuild" / "numerge.config");
            EnsureCleanDirectory(Parameters.NugetRoot);
            if(!Numerge.NugetPackageMerger.Merge(Parameters.NugetIntermediateRoot, Parameters.NugetRoot, config,
                new NumergeNukeLogger()))
                throw new Exception("Package merge failed");
        });
    
    Target RunTests => _ => _
        .DependsOn(RunCoreLibsTests)
        .DependsOn(RunRenderTests)
        .DependsOn(RunDesignerTests)
        .DependsOn(RunLeakTests);
    
    Target Package => _ => _
        .DependsOn(RunTests)
        .DependsOn(CreateNugetPackages);
    
    Target CiAzureLinux => _ => _
        .DependsOn(RunTests);
    
    Target CiAzureOSX => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);
    
    Target CiAzureWindows => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);

    
    public static int Main() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Execute<Build>(x => x.Package)
            : Execute<Build>(x => x.RunTests);

}
