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
using System.IO.Compression;

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

    Target CompileNative => _ => _
    .DependsOn(Clean)
    .OnlyWhen(() => EnvironmentInfo.IsOsx)
    .Executes(() =>
    {
        var project = $"{RootDirectory}/native/Avalonia.Native/src/OSX/Avalonia.Native.OSX.xcodeproj/";
        var args = $"-project {project} -configuration {Parameters.Configuration} CONFIGURATION_BUILD_DIR={RootDirectory}/Build/Products/Release";
        ProcessTasks.StartProcess("xcodebuild", args).AssertZeroExitCode();
    });

    Target Compile => _ => _
        .DependsOn(Clean, CompileNative, DownloadAvaloniaNativeLib)
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
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Nuke.Common.BuildServers.TeamCity.Instance == null)// no direct2d tests on teamcity - they fail?
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

    Target UpdateTeamCityVersion => _ => _
        .Executes(() =>
        {
            Nuke.Common.BuildServers.TeamCity.Instance?.SetBuildNumber(Parameters.Version);
        });

    Target DownloadAvaloniaNativeLib => _ => _
        .After(Clean)
        .OnlyWhen(() => EnvironmentInfo.IsWin)
        .Executes(() =>
        {
            //download avalonia native osx binary, so we don't have to build it on osx
            //expected to be -> Build/Products/Release/libAvalonia.Native.OSX.dylib
            //Avalonia.Native.0.9.999-cibuild0005383-beta.nupkg
            string nugetversion = "0.9.2.16";

            var nugetdir = RootDirectory + "/Build/Products/Release/";
            string nugeturl = "https://www.myget.org/F/avalonia-ci/api/v2/package/Avalonia.Native/";
            //string nugeturl = "https://www.nuget.org/api/v2/package/Avalonia.Native/";

            nugeturl += nugetversion;

            //myget packages are expiring so i've made a copy here
            //google drive file share https://drive.google.com/open?id=1HK-XfBZRunGpxXcGUUEC-64H9T_n9cIJ
            //nugeturl = "https://drive.google.com/uc?id=1HK-XfBZRunGpxXcGUUEC-64H9T_n9cIJ&export=download";//Avalonia.Native.0.9.999-cibuild0005383-beta
            nugeturl = "https://drive.google.com/uc?id=1fNKJ-KNsPtoi_MYVJZ0l4hbgHAkLMYZZ&export=download";//Avalonia.Native.0.9.2.16.nupkg custom build
            string nugetname = $"Avalonia.Native.{nugetversion}";
            string nugetcontentsdir = Path.Combine(nugetdir, nugetname);
            string nugetpath = nugetcontentsdir + ".nupkg";
            Logger.Info($"Downloading {nugetname} from {nugeturl}");
            Nuke.Common.IO.HttpTasks.HttpDownloadFile(nugeturl, nugetpath);
            System.IO.Compression.ZipFile.ExtractToDirectory(nugetpath, nugetcontentsdir, true);

            CopyFile(nugetcontentsdir + @"/runtimes/osx/native/libAvaloniaNative.dylib", nugetdir + "libAvalonia.Native.OSX." +
                "dylib", FileExistsPolicy.Overwrite);
        });

    Target CreateIntermediateNugetPackages => _ => _
        .DependsOn(Compile)
        .DependsOn(DownloadAvaloniaNativeLib)
        .After(RunTests)
        .Executes(() =>
        {
            if (Parameters.IsRunningOnWindows)

                MsBuildCommon(Parameters.MSBuildSolution, c => c
                    .AddProperty("PackAvaloniaNative", "true")
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


    Target PublishLocalNugetPackages => _ => _
    .DependsOn(CreateNugetPackages)
    .Executes(() =>
    {
        string nugetPackagesDir = Variable("NUGET_PACKAGES") ?? Path.Combine(Variable("USERPROFILE") ?? Variable("HOME"), ".nuget/packages");

        foreach (var package in Directory.EnumerateFiles(Parameters.NugetRoot))
        {
            var packName = Path.GetFileName(package);
            string packgageFolderName = packName.Replace($".{Parameters.Version}.nupkg", "");
            var nugetCaheFolder = Path.Combine(nugetPackagesDir, packgageFolderName, Parameters.Version);

            //clean directory is not good, nuget will noticed and clean our files
            //EnsureCleanDirectory(nugetCaheFolder);
            EnsureExistingDirectory(nugetCaheFolder);

            CopyFile(package, nugetCaheFolder + "/" + packName, FileExistsPolicy.Skip);

            Logger.Info($"Extracting to {nugetCaheFolder}, {package}");

            ZipFile.ExtractToDirectory(package, nugetCaheFolder, true);
        }
    });

    Target ClearLocalNugetPackages => _ => _
    .Executes(() =>
    {
        string nugetPackagesDir = Variable("NUGET_PACKAGES") ?? Path.Combine(Variable("USERPROFILE") ?? Variable("HOME"), ".nuget/packages");

        foreach (var package in Directory.EnumerateFiles(Parameters.NugetRoot))
        {
            var packName = Path.GetFileName(package);
            string packgageFolderName = packName.Replace($".{Parameters.Version}.nupkg", "");
            var nugetCaheFolder = Path.Combine(nugetPackagesDir, packgageFolderName, Parameters.Version);

            EnsureCleanDirectory(nugetCaheFolder);
        }
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
