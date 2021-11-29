using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
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
    [Solution("Avalonia.sln")] readonly Solution Solution;

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
    }

    IReadOnlyCollection<Output> MsBuildCommon(
        string projectFile,
        Configure<MSBuildSettings> configurator = null)
    {
        return MSBuild(c => c
            .SetProjectFile(projectFile)
            // This is required for VS2019 image on Azure Pipelines
            .When(Parameters.IsRunningOnWindows &&
                  Parameters.IsRunningOnAzure, _ => _
                .AddProperty("JavaSdkDirectory", GetVariable<string>("JAVA_HOME_8_X64")))
            .AddProperty("PackageVersion", Parameters.Version)
            .AddProperty("iOSRoslynPathHackRequired", true)
            .SetProcessToolPath(MsBuildExe.Value)
            .SetConfiguration(Parameters.Configuration)
            .SetVerbosity(MSBuildVerbosity.Minimal)
            .Apply(configurator));
    }

    Target Clean => _ => _.Executes(() =>
    {
        Parameters.BuildDirs.ForEach(DeleteDirectory);
        Parameters.BuildDirs.ForEach(EnsureCleanDirectory);
        EnsureCleanDirectory(Parameters.ArtifactsDir);
        EnsureCleanDirectory(Parameters.NugetIntermediateRoot);
        EnsureCleanDirectory(Parameters.NugetRoot);
        EnsureCleanDirectory(Parameters.ZipRoot);
        EnsureCleanDirectory(Parameters.TestResultsRoot);
    });

    Target CompileHtmlPreviewer => _ => _
        .DependsOn(Clean)
        .OnlyWhenStatic(() => !Parameters.SkipPreviewer)
        .Executes(() =>
        {
            var webappDir = RootDirectory / "src" / "Avalonia.DesignerSupport" / "Remote" / "HtmlTransport" / "webapp";

            NpmTasks.NpmInstall(c => c
                .SetProcessWorkingDirectory(webappDir)
                .SetProcessArgumentConfigurator(a => a.Add("--silent")));
            NpmTasks.NpmRun(c => c
                .SetProcessWorkingDirectory(webappDir)
                .SetCommand("dist"));
        });

    Target CompileNative => _ => _
        .DependsOn(Clean)
        .DependsOn(GenerateCppHeaders)
        .OnlyWhenStatic(() => EnvironmentInfo.IsOsx)
        .Executes(() =>
        {
            var project = $"{RootDirectory}/native/Avalonia.Native/src/OSX/Avalonia.Native.OSX.xcodeproj/";
            var args = $"-project {project} -configuration {Parameters.Configuration} CONFIGURATION_BUILD_DIR={RootDirectory}/Build/Products/Release";
            ProcessTasks.StartProcess("xcodebuild", args).AssertZeroExitCode();
        });

    Target Compile => _ => _
        .DependsOn(Clean, CompileNative)
        .DependsOn(CompileHtmlPreviewer)
        .Executes(async () =>
        {
            if (Parameters.IsRunningOnWindows)
                MsBuildCommon(Parameters.MSBuildSolution, c => c
                    .SetProcessArgumentConfigurator(a => a.Add("/r"))
                    .AddTargets("Build")
                );

            else
                DotNetBuild(c => c
                    .SetProjectFile(Parameters.MSBuildSolution)
                    .AddProperty("PackageVersion", Parameters.Version)
                    .SetConfiguration(Parameters.Configuration)
                );
        });

    void RunCoreTest(string projectName)
    {
        Information($"Running tests from {projectName}");
        var project = Solution.GetProject(projectName).NotNull("project != null");

        foreach (var fw in project.GetTargetFrameworks())
        {
            if (fw.StartsWith("net4")
                && RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                && Environment.GetEnvironmentVariable("FORCE_LINUX_TESTS") != "1")
            {
                Information($"Skipping {projectName} ({fw}) tests on Linux - https://github.com/mono/mono/issues/13969");
                continue;
            }

            Information($"Running for {projectName} ({fw}) ...");

            DotNetTest(c => c
                .SetProjectFile(project)
                .SetConfiguration(Parameters.Configuration)
                .SetFramework(fw)
                .EnableNoBuild()
                .EnableNoRestore()
                .When(Parameters.PublishTestResults, _ => _
                    .SetLogger("trx")
                    .SetResultsDirectory(Parameters.TestResultsRoot)));
        }
    }

    Target RunHtmlPreviewerTests => _ => _
        .DependsOn(CompileHtmlPreviewer)
        .OnlyWhenStatic(() => !(Parameters.SkipPreviewer || Parameters.SkipTests))
        .Executes(() =>
        {
            var webappTestDir = RootDirectory / "tests" / "Avalonia.DesignerSupport.Tests" / "Remote" / "HtmlTransport" / "webapp";

            NpmTasks.NpmInstall(c => c
                .SetProcessWorkingDirectory(webappTestDir)
                .SetProcessArgumentConfigurator(a => a.Add("--silent")));
            NpmTasks.NpmRun(c => c
                .SetProcessWorkingDirectory(webappTestDir)
                .SetCommand("test"));
        });

    Target RunCoreLibsTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("Avalonia.Animation.UnitTests");
            RunCoreTest("Avalonia.Base.UnitTests");
            RunCoreTest("Avalonia.Controls.UnitTests");
            RunCoreTest("Avalonia.Controls.DataGrid.UnitTests");
            RunCoreTest("Avalonia.Input.UnitTests");
            RunCoreTest("Avalonia.Interactivity.UnitTests");
            RunCoreTest("Avalonia.Layout.UnitTests");
            RunCoreTest("Avalonia.Markup.UnitTests");
            RunCoreTest("Avalonia.Markup.Xaml.UnitTests");
            RunCoreTest("Avalonia.Styling.UnitTests");
            RunCoreTest("Avalonia.Visuals.UnitTests");
            RunCoreTest("Avalonia.Skia.UnitTests");
            RunCoreTest("Avalonia.ReactiveUI.UnitTests");
        });

    Target RunRenderTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("Avalonia.Skia.RenderTests");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                RunCoreTest("Avalonia.Direct2D1.RenderTests");
        });

    Target RunDesignerTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("Avalonia.DesignerSupport.Tests");
        });

    [PackageExecutable("JetBrains.dotMemoryUnit", "dotMemoryUnit.exe")] readonly Tool DotMemoryUnit;

    Target RunLeakTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
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
            var pathToProjectSource = RootDirectory / "samples" / "ControlCatalog.NetCore";
            var pathToPublish = pathToProjectSource / "bin" / data.Configuration / "publish";

            DotNetPublish(c => c
                .SetProject(pathToProjectSource / "ControlCatalog.NetCore.csproj")
                .EnableNoBuild()
                .SetConfiguration(data.Configuration)
                .AddProperty("PackageVersion", data.Version)
                .AddProperty("PublishDir", pathToPublish));

            Zip(data.ZipCoreArtifacts, data.BinRoot);
            Zip(data.ZipNuGetArtifacts, data.NugetRoot);
            Zip(data.ZipTargetControlCatalogNetCoreDir, pathToPublish);
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
                DotNetPack(c => c
                    .SetProject(Parameters.MSBuildSolution)
                    .SetConfiguration(Parameters.Configuration)
                    .AddProperty("PackageVersion", Parameters.Version));
        });

    Target CreateNugetPackages => _ => _
        .DependsOn(CreateIntermediateNugetPackages)
        .Executes(() =>
        {
            BuildTasksPatcher.PatchBuildTasksInPackage(Parameters.NugetIntermediateRoot / "Avalonia.Build.Tasks." +
                                                       Parameters.Version + ".nupkg");
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
        .DependsOn(RunHtmlPreviewerTests)
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

public static class ToolSettingsExtensions
{
    public static T Apply<T>(this T settings, Configure<T> configurator)
    {
        return configurator != null ? configurator(settings) : settings;
    }
}
