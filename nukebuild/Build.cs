using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.MSBuild.MSBuildTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.Xunit.XunitTasks;
using static Nuke.Common.Tools.VSWhere.VSWhereTasks;
using static Serilog.Log;
using MicroCom.CodeGenerator;
using Nuke.Common.IO;

/*
 Before editing this file, install support plugin for your IDE,
 running and debugging a particular target (optionally without deps) would be way easier
 ReSharper/Rider - https://plugins.jetbrains.com/plugin/10803-nuke-support
 VSCode - https://marketplace.visualstudio.com/items?itemName=nuke.support

 */

partial class Build : NukeBuild
{
    BuildParameters Parameters { get; set; }

    [PackageExecutable("Microsoft.DotNet.ApiCompat.Tool", "Microsoft.DotNet.ApiCompat.Tool.dll", Framework = "net6.0")]
    Tool ApiCompatTool;
    
    [PackageExecutable("Microsoft.DotNet.GenAPI.Tool", "Microsoft.DotNet.GenAPI.Tool.dll", Framework = "net8.0")]
    Tool ApiGenTool;

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
        ExecWait("dotnet version:", "dotnet", "--info");
        ExecWait("dotnet workloads:", "dotnet", "workload list");
        Information("Processor count: " + Environment.ProcessorCount);
        Information("Available RAM: " + GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 0x100000 + "MB");
    }

    DotNetConfigHelper ApplySettingCore(DotNetConfigHelper c)
    {
        if (Parameters.IsRunningOnAzure)
            c.AddProperty("JavaSdkDirectory", GetVariable<string>("JAVA_HOME_11_X64"));
        c.AddProperty("PackageVersion", Parameters.Version)
            .SetConfiguration(Parameters.Configuration)
            .SetVerbosity(DotNetVerbosity.Minimal);
        return c;
    }
    DotNetBuildSettings ApplySetting(DotNetBuildSettings c, Configure<DotNetBuildSettings> configurator = null) =>
        ApplySettingCore(c).Build.Apply(configurator);

    DotNetPackSettings ApplySetting(DotNetPackSettings c, Configure<DotNetPackSettings> configurator = null) =>
        ApplySettingCore(c).Pack.Apply(configurator);

    DotNetTestSettings ApplySetting(DotNetTestSettings c, Configure<DotNetTestSettings> configurator = null) =>
        ApplySettingCore(c).Test.Apply(configurator);

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
        .Executes(() =>
        {
            DotNetBuild(c => ApplySetting(c)
                .SetProjectFile(Parameters.MSBuildSolution)
            );
        });

    void RunCoreTest(string projectName)
    {
        Information($"Running tests from {projectName}");
        var project = RootDirectory.GlobFiles(@$"**\{projectName}.csproj").FirstOrDefault()
            ?? throw new InvalidOperationException($"Project {projectName} doesn't exist");

        // Nuke and MSBuild tools have build-in helpers to get target frameworks from the project.
        // Unfortunately, it gets broken with every second SDK update, so we had to do it manually.
        var fileXml = XDocument.Parse(File.ReadAllText(project));
        var targetFrameworks = fileXml.Descendants("TargetFrameworks")
            .FirstOrDefault()?.Value.Split(';').Select(f => f.Trim());
        if (targetFrameworks is null)
        {
            var targetFramework = fileXml.Descendants("TargetFramework").FirstOrDefault()?.Value;
            if (targetFramework is not null)
            {
                targetFrameworks = new[] { targetFramework };
            }
        }
        if (targetFrameworks is null)
        {
            throw new InvalidOperationException("No target frameworks were found in the test project");
        }

        foreach (var fw in targetFrameworks)
        {
            if (fw.StartsWith("net4")
                && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                && Environment.GetEnvironmentVariable("FORCE_LINUX_TESTS") != "1")
            {
                Information($"Skipping {projectName} ({fw}) tests on *nix - https://github.com/mono/mono/issues/13969");
                continue;
            }

            Information($"Running for {projectName} ({fw}) ...");

            DotNetTest(c => ApplySetting(c)
                .SetProjectFile(project)
                .SetFramework(fw)
                .EnableNoBuild()
                .EnableNoRestore()
                .When(Parameters.PublishTestResults, _ => _
                    .SetLoggers("trx")
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
            RunCoreTest("Avalonia.Base.UnitTests");
            RunCoreTest("Avalonia.Controls.UnitTests");
            RunCoreTest("Avalonia.Controls.DataGrid.UnitTests");
            RunCoreTest("Avalonia.Markup.UnitTests");
            RunCoreTest("Avalonia.Markup.Xaml.UnitTests");
            RunCoreTest("Avalonia.Skia.UnitTests");
            RunCoreTest("Avalonia.ReactiveUI.UnitTests");
            RunCoreTest("Avalonia.Headless.NUnit.UnitTests");
            RunCoreTest("Avalonia.Headless.XUnit.UnitTests");
        });

    Target RunRenderTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("Avalonia.Skia.RenderTests");
            if (Parameters.IsRunningOnWindows)
                RunCoreTest("Avalonia.Direct2D1.RenderTests");
        });

    Target RunToolsTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests)
        .DependsOn(Compile)
        .Executes(() =>
        {
            RunCoreTest("Avalonia.Generators.Tests");
            if (Parameters.IsRunningOnWindows)
                RunCoreTest("Avalonia.DesignerSupport.Tests");
        });

    Target RunLeakTests => _ => _
        .OnlyWhenStatic(() => !Parameters.SkipTests && Parameters.IsRunningOnWindows)
        .DependsOn(Compile)
        .Executes(() =>
        {
            void DoMemoryTest()
            {
                RunCoreTest("Avalonia.LeakTests");
            }
            ControlFlow.ExecuteWithRetry(DoMemoryTest, delay: TimeSpan.FromMilliseconds(3));
        });

    Target ZipFiles => _ => _
        .After(CreateNugetPackages, Compile, RunCoreLibsTests, Package)
        .Executes(() =>
        {
            var data = Parameters;
            Zip(data.ZipNuGetArtifacts, data.NugetRoot);
        });

    Target CreateIntermediateNugetPackages => _ => _
        .DependsOn(Compile)
        .After(RunTests)
        .Executes(() =>
        {
            DotNetPack(c => ApplySetting(c).SetProject(Parameters.MSBuildSolution));
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
            RefAssemblyGenerator.GenerateRefAsmsInPackage(
                Parameters.NugetRoot / $"Avalonia.{Parameters.Version}.nupkg",
                Parameters.NugetRoot / $"Avalonia.{Parameters.Version}.snupkg");
        });
    
    Target ValidateApiDiff => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(async () =>
        {
            await Task.WhenAll(
                Directory.GetFiles(Parameters.NugetRoot, "*.nupkg").Select(nugetPackage => ApiDiffHelper.ValidatePackage(
                    ApiCompatTool, nugetPackage, Parameters.ApiValidationBaseline,
                    Parameters.ApiValidationSuppressionFiles, Parameters.UpdateApiValidationSuppression)));
        });
    
    Target OutputApiDiff => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(async () =>
        {
            await Task.WhenAll(
                Directory.GetFiles(Parameters.NugetRoot, "*.nupkg").Select(nugetPackage => ApiDiffHelper.GetDiff(
                    ApiGenTool, RootDirectory / "api" / "diff",
                    nugetPackage, Parameters.ApiValidationBaseline)));
        });
    
    Target RunTests => _ => _
        .DependsOn(RunCoreLibsTests)
        .DependsOn(RunRenderTests)
        .DependsOn(RunToolsTests)
        .DependsOn(RunHtmlPreviewerTests)
        .DependsOn(RunLeakTests);

    Target Package => _ => _
        .DependsOn(RunTests)
        .DependsOn(CreateNugetPackages)
        .DependsOn(ValidateApiDiff);

    Target CiAzureLinux => _ => _
        .DependsOn(RunTests);

    Target CiAzureOSX => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);

    Target CiAzureWindows => _ => _
        .DependsOn(Package)
        .DependsOn(ZipFiles);

    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "Avalonia.Native" / "avn.idl"));
        File.WriteAllText(RootDirectory / "native" / "Avalonia.Native" / "inc" / "avalonia-native.h",
            file.GenerateCppHeader());
    });


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
