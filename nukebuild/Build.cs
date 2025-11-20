using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Npm;
using Nuke.Common.Utilities;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotMemoryUnit.DotMemoryUnitTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;
using MicroCom.CodeGenerator;
using NuGet.Configuration;
using NuGet.Versioning;
using Nuke.Common.CI.AzurePipelines;
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

#nullable enable
    ApiDiffHelper.GlobalDiffInfo? GlobalDiff { get; set; }
#nullable restore

    [NuGetPackage("Microsoft.DotNet.ApiCompat.Tool", "Microsoft.DotNet.ApiCompat.Tool.dll", Framework = "net8.0")]
    Tool ApiCompatTool;
    
    [NuGetPackage("Microsoft.DotNet.ApiDiff.Tool", "Microsoft.DotNet.ApiDiff.Tool.dll", Framework = "net8.0")]
    Tool ApiDiffTool;

    [NuGetPackage("dotnet-ilrepack", "ILRepackTool.dll", Framework = "net8.0")]
    Tool IlRepackTool;
    
    protected override void OnBuildInitialized()
    {
        Parameters = new BuildParameters(this, ScheduledTargets.Contains(BuildToNuGetCache));

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

        if (Host is AzurePipelines azurePipelines)
            azurePipelines.UpdateBuildNumber(Parameters.Version);
    }

    DotNetConfigHelper ApplySettingCore(DotNetConfigHelper c)
    {
        if (Parameters.IsRunningOnAzure)
            c.AddProperty("JavaSdkDirectory", GetVariable<string>("JAVA_HOME_11_X64"));
        c.AddProperty("PackageVersion", Parameters.Version)
            .SetConfiguration(Parameters.Configuration)
            .SetVerbosity(DotNetVerbosity.minimal);
        if (Parameters.IsPackingToLocalCache)
            c
                .AddProperty("ForcePackAvaloniaNative", "True")
                .AddProperty("SkipObscurePlatforms", "True")
                .AddProperty("SkipBuildingSamples", "True")
                .AddProperty("SkipBuildingTests", "True");
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
        foreach (var buildDir in Parameters.BuildDirs)
        {
            Information("Deleting {Directory}", buildDir);
            buildDir.DeleteDirectory();
        }

        CleanDirectory(Parameters.ArtifactsDir);
        CleanDirectory(Parameters.NugetIntermediateRoot);
        CleanDirectory(Parameters.NugetRoot);
        CleanDirectory(Parameters.ZipRoot);
        CleanDirectory(Parameters.TestResultsRoot);

        void CleanDirectory(AbsolutePath path)
        {
            Information("Cleaning {Path}", path);
            path.CreateOrCleanDirectory();
        }
    });

    Target CompileHtmlPreviewer => _ => _
        .DependsOn(Clean)
        .OnlyWhenStatic(() => !Parameters.SkipPreviewer)
        .Executes(() =>
        {
            var webappDir = RootDirectory / "src" / "Avalonia.DesignerSupport" / "Remote" / "HtmlTransport" / "webapp";

            NpmTasks.NpmInstall(c => c
                .SetProcessWorkingDirectory(webappDir)
                .SetProcessAdditionalArguments("--silent"));
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

    Target OutputVersion => _ => _
        .Requires(() => VersionOutputDir)
        .Executes(() =>
        {
            var versionFile = Path.Combine(Parameters.VersionOutputDir, "version.txt");
            var currentBuildVersion = Parameters.Version;
            Console.WriteLine("Version is: " + currentBuildVersion);
            File.WriteAllText(versionFile, currentBuildVersion);

            var prIdFile = Path.Combine(Parameters.VersionOutputDir, "prId.txt");
            var prId = Environment.GetEnvironmentVariable("SYSTEM_PULLREQUEST_PULLREQUESTNUMBER");
            Console.WriteLine("PR Number  is: " + prId);
            File.WriteAllText(prIdFile, prId);
        });

    void RunCoreTest(string projectName)
    {
        RunCoreTest(projectName, (project, tfm) =>
        {
            DotNetTest(c => ApplySetting(c, project,tfm));
        });
    }

    void RunCoreDotMemoryUnit(string projectName)
    {
        RunCoreTest(projectName, (project, tfm) =>
        {
            var testSettings = ApplySetting(new DotNetTestSettings(), project, tfm);
            var testToolPath = GetToolPathInternal(new DotNetTasks(), testSettings);
            var testArgs = GetArguments(testSettings).JoinSpace();
            DotMemoryUnit($"{testToolPath} --propagate-exit-code -- {testArgs:nq}");
        });

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(GetToolPathInternal))]
        extern static string GetToolPathInternal(ToolTasks tasks, ToolOptions options);

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = nameof(GetArguments))]
        extern static IEnumerable<string> GetArguments(ToolOptions options);
    }

    void RunCoreTest(string projectName, Action<string, string> runTest)
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
            var tfm = fw;
            if (tfm == "$(AvsCurrentTargetFramework)")
            {
                tfm = "net10.0";
            }
            if (tfm == "$(AvsLegacyTargetFrameworks)")
            {
                tfm = "net8.0";
            }
            
            if (tfm.StartsWith("net4")
                && (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                && Environment.GetEnvironmentVariable("FORCE_LINUX_TESTS") != "1")
            {
                Information($"Skipping {projectName} ({tfm}) tests on *nix - https://github.com/mono/mono/issues/13969");
                continue;
            }

            Information($"Running for {projectName} ({tfm}) ...");

            runTest(project, tfm);
        }
    }

    DotNetTestSettings ApplySetting(DotNetTestSettings settings, string project, string tfm) =>
        ApplySetting(settings)
        .SetProjectFile(project)
        .SetFramework(tfm)
        .EnableNoBuild()
        .EnableNoRestore()
        .When(_ => Parameters.PublishTestResults, _ => _
            .SetLoggers("trx")
            .SetResultsDirectory(Parameters.TestResultsRoot));

    Target RunHtmlPreviewerTests => _ => _
        .DependsOn(CompileHtmlPreviewer)
        .OnlyWhenStatic(() => !(Parameters.SkipPreviewer || Parameters.SkipTests))
        .Executes(() =>
        {
            var webappTestDir = RootDirectory / "tests" / "Avalonia.DesignerSupport.Tests" / "Remote" / "HtmlTransport" / "webapp";

            NpmTasks.NpmInstall(c => c
                .SetProcessWorkingDirectory(webappTestDir)
                .SetProcessAdditionalArguments("--silent"));
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
            RunCoreTest("Avalonia.Markup.UnitTests");
            RunCoreTest("Avalonia.Markup.Xaml.UnitTests");
            RunCoreTest("Avalonia.Skia.UnitTests");
            RunCoreTest("Avalonia.Headless.NUnit.PerAssembly.UnitTests");
            RunCoreTest("Avalonia.Headless.NUnit.PerTest.UnitTests");
            RunCoreTest("Avalonia.Headless.XUnit.PerAssembly.UnitTests");
            RunCoreTest("Avalonia.Headless.XUnit.PerTest.UnitTests");
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
                RunCoreDotMemoryUnit("Avalonia.LeakTests");
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
                                                       Parameters.Version + ".nupkg",
                                                       IlRepackTool);
            var config = Numerge.MergeConfiguration.LoadFile(RootDirectory / "nukebuild" / "numerge.config");
            Parameters.NugetRoot.CreateOrCleanDirectory();
            if(!Numerge.NugetPackageMerger.Merge(Parameters.NugetIntermediateRoot, Parameters.NugetRoot, config,
                new NumergeNukeLogger()))
                throw new Exception("Package merge failed");
            RefAssemblyGenerator.GenerateRefAsmsInPackage(
                Parameters.NugetRoot / $"Avalonia.{Parameters.Version}.nupkg",
                Parameters.NugetRoot / $"Avalonia.{Parameters.Version}.snupkg");
        });

    Target DownloadApiBaselinePackages => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(async () =>
        {
            GlobalDiff = await ApiDiffHelper.DownloadAndExtractPackagesAsync(
                Directory.EnumerateFiles(Parameters.NugetRoot, "*.nupkg").Select(path => (AbsolutePath)path),
                NuGetVersion.Parse(Parameters.Version),
                Parameters.IsReleaseBranch,
                Parameters.ArtifactsDir / "api-diff" / "assemblies",
                Parameters.ForceApiValidationBaseline is { } forcedBaseline ? NuGetVersion.Parse(forcedBaseline) : null);
        });

    Target ValidateApiDiff => _ => _
        .DependsOn(DownloadApiBaselinePackages)
        .Executes(() =>
        {
            var globalDiff = GlobalDiff!;

            Parallel.ForEach(
                globalDiff.Packages,
                packageDiff => ApiDiffHelper.ValidatePackage(
                    ApiCompatTool,
                    packageDiff,
                    Parameters.ArtifactsDir / "api-diff" / "assemblies",
                    Parameters.ApiValidationSuppressionFiles,
                    Parameters.UpdateApiValidationSuppression));
        });
    
    Target OutputApiDiff => _ => _
        .DependsOn(DownloadApiBaselinePackages)
        .Executes(() =>
        {
            var globalDiff = GlobalDiff!;
            var outputFolderPath = Parameters.ArtifactsDir / "api-diff" / "markdown";
            var baselineDisplay = globalDiff.BaselineVersion.ToString();
            var currentDisplay = globalDiff.CurrentVersion.ToString();

            Parallel.ForEach(
                globalDiff.Packages,
                packageDiff => ApiDiffHelper.GenerateMarkdownDiff(
                    ApiDiffTool,
                    packageDiff,
                    outputFolderPath,
                    baselineDisplay,
                    currentDisplay));

            ApiDiffHelper.MergePackageMarkdownDiffFiles(outputFolderPath, baselineDisplay, currentDisplay);
        });

    Target RunTests => _ => _
        .DependsOn(RunCoreLibsTests)
        .DependsOn(RunRenderTests)
        .DependsOn(RunToolsTests)
        .DependsOn(RunHtmlPreviewerTests);
        //.DependsOn(RunLeakTests); // dotMemory Unit doesn't support modern .NET versions, see https://youtrack.jetbrains.com/issue/DMU-300/

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
        .DependsOn(VerifyXamlCompilation)
        .DependsOn(ZipFiles);

    Target BuildToNuGetCache => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(() =>
        {
            if (!Parameters.IsPackingToLocalCache)
                throw new InvalidOperationException();

            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(
                Settings.LoadDefaultSettings(RootDirectory));
            
            foreach (var path in Parameters.NugetRoot.GlobFiles("*.nupkg"))
            {
                using var f = File.Open(path.ToString(), FileMode.Open, FileAccess.Read);
                using var zip = new ZipArchive(f, ZipArchiveMode.Read);
                var nuspecEntry = zip.Entries.First(e => e.FullName.EndsWith(".nuspec") && e.FullName == e.Name);
                var packageId = XDocument.Load(nuspecEntry.Open()).Document.Root
                    .Elements().First(x => x.Name.LocalName == "metadata")
                    .Elements().First(x => x.Name.LocalName == "id").Value;

                var packagePath = Path.Combine(
                    globalPackagesFolder,
                    packageId.ToLowerInvariant(),
                    BuildParameters.LocalBuildVersion);

                if (Directory.Exists(packagePath))
                    Directory.Delete(packagePath, true);
                Directory.CreateDirectory(packagePath);
                zip.ExtractToDirectory(packagePath);
                File.WriteAllText(Path.Combine(packagePath, ".nupkg.metadata"), @"{
  ""version"": 2,
  ""contentHash"": ""e900dFK7jHJ2WcprLcgJYQoOMc6ejRTwAAMi0VGOFbSczcF98ZDaqwoQIiyqpAwnja59FSbV+GUUXfc3vaQ2Jg=="",
  ""source"": ""https://api.nuget.org/v3/index.json""
}");
            }
        });

    Target GenerateCppHeaders => _ => _.Executes(() =>
    {
        var file = MicroComCodeGenerator.Parse(
            File.ReadAllText(RootDirectory / "src" / "Avalonia.Native" / "avn.idl"));
        File.WriteAllText(RootDirectory / "native" / "Avalonia.Native" / "inc" / "avalonia-native.h",
            file.GenerateCppHeader());
    });

    Target VerifyXamlCompilation => _ => _
        .DependsOn(CreateNugetPackages)
        .Executes(() =>
        {
            var buildTestsDirectory = RootDirectory / "tests" / "BuildTests";
            var artifactsDirectory = buildTestsDirectory / "artifacts";
            var nugetCacheDirectory = artifactsDirectory / "nuget-cache";

            artifactsDirectory.DeleteDirectory();
            BuildTestsAndVerify("Debug");
            BuildTestsAndVerify("Release");

            void BuildTestsAndVerify(string configuration)
            {
                var configName = configuration.ToLowerInvariant();

                DotNetBuild(settings => settings
                    .SetConfiguration(configuration)
                    .SetProperty("AvaloniaVersion", Parameters.Version)
                    .SetProperty("NuGetPackageRoot", nugetCacheDirectory)
                    .SetPackageDirectory(nugetCacheDirectory)
                    .SetProjectFile(buildTestsDirectory / "BuildTests.sln")
                    .SetProcessAdditionalArguments("--nodeReuse:false"));

                // Standard compilation - should have compiled XAML
                VerifyBuildTestAssembly("bin", "BuildTests");
                VerifyBuildTestAssembly("bin", "BuildTests.Android");
                VerifyBuildTestAssembly("bin", "BuildTests.Browser");
                VerifyBuildTestAssembly("bin", "BuildTests.Desktop");
                VerifyBuildTestAssembly("bin", "BuildTests.FSharp");
                VerifyBuildTestAssembly("bin", "BuildTests.iOS");
                VerifyBuildTestAssembly("bin", "BuildTests.WpfHybrid");

                // Publish previously built project without rebuilding - should have compiled XAML
                PublishBuildTestProject("BuildTests.Desktop", noBuild: true);
                VerifyBuildTestAssembly("publish", "BuildTests.Desktop");

                // Publish NativeAOT build, then run it - should not crash and have the expected output
                PublishBuildTestProject("BuildTests.NativeAot");
                var exeExtension = OperatingSystem.IsWindows() ? ".exe" : null;
                XamlCompilationVerifier.VerifyNativeAot(
                    GetBuildTestOutputPath("publish", "BuildTests.NativeAot", exeExtension));

                void PublishBuildTestProject(string projectName, bool? noBuild = null)
                    => DotNetPublish(settings => settings
                        .SetConfiguration(configuration)
                        .SetProperty("AvaloniaVersion", Parameters.Version)
                        .SetProperty("NuGetPackageRoot", nugetCacheDirectory)
                        .SetPackageDirectory(nugetCacheDirectory)
                        .SetNoBuild(noBuild)
                        .SetProject(buildTestsDirectory / projectName / (projectName + ".csproj"))
                        .SetProcessAdditionalArguments("--nodeReuse:false"));

                void VerifyBuildTestAssembly(string folder, string projectName)
                    => XamlCompilationVerifier.VerifyAssemblyCompiledXaml(
                        GetBuildTestOutputPath(folder, projectName, ".dll"));

                AbsolutePath GetBuildTestOutputPath(string folder, string projectName, string extension)
                    => artifactsDirectory / folder / projectName / configName / (projectName + extension);
            }
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
