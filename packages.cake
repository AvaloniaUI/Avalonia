using System.Xml.Linq;

public class Packages
{
    public List<NuGetPackSettings> NuspecNuGetSettings { get; private set; }
    public FilePath[] NugetPackages { get; private set; }
    public FilePath[] BinFiles { get; private set; }
    public string NugetPackagesDir {get; private set;}
    public string SkiaSharpVersion {get; private set; }
    public string SkiaSharpLinuxVersion {get; private set; }
    public Dictionary<string, IList<Tuple<string,string>>> PackageVersions{get; private set;}
    
       
    
    class DependencyBuilder : List<NuSpecDependency>
    {
        Packages _parent;
        public DependencyBuilder(Packages parent)
        {
            _parent = parent;
        }
        
        string GetVersion(string name)
        {
            return _parent.PackageVersions[name].First().Item1;
        }
        
        
        public DependencyBuilder Dep(string name, params string[] fws)
        {
            if(fws.Length == 0)
                Add(new NuSpecDependency() { Id = name, Version = GetVersion(name) });
            foreach(var fw in fws)
                Add(new NuSpecDependency() { Id = name, TargetFramework = fw, Version = GetVersion(name) });
            return this;
        }
        public DependencyBuilder Deps(string[] fws, params string[] deps)
        {
            foreach(var fw in fws)
                foreach(var name in deps)
                    Add(new NuSpecDependency() { Id = name, TargetFramework = fw, Version = GetVersion(name) });
            return this;
        }
    }
        
    public Packages(ICakeContext context, Parameters parameters)
    {
        // NUGET NUSPECS
        context.Information("Getting git modules:");

        var ignoredSubModulesPaths = System.IO.File.ReadAllLines(".git/config").Where(m=>m.StartsWith("[submodule ")).Select(m => 
        {
            var path = m.Split(' ')[1].Trim("\"[] \t".ToArray());
            context.Information(path);
            return ((DirectoryPath)context.Directory(path)).FullPath;
        }).ToList();

        var normalizePath = new Func<string, string>(
            path => path.Replace(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar).ToUpperInvariant());

        // Key: Package Id
        // Value is Tuple where Item1: Package Version, Item2: The *.csproj/*.props file path.
        var packageVersions = new Dictionary<string, IList<Tuple<string,string>>>();
        PackageVersions = packageVersions;
        System.IO.Directory.EnumerateFiles(((DirectoryPath)context.Directory("./build")).FullPath,
            "*.props", SearchOption.AllDirectories).ToList().ForEach(fileName =>
        {
            if (!ignoredSubModulesPaths.Any(i => normalizePath(fileName).Contains(normalizePath(i))))
            {
                var xdoc = XDocument.Load(fileName);
                foreach (var reference in xdoc.Descendants().Where(x => x.Name.LocalName == "PackageReference"))
                {
                    var name = reference.Attribute("Include").Value;
                    var versionAttribute = reference.Attribute("Version");
                    var version = versionAttribute != null 
                        ? versionAttribute.Value 
                        : reference.Elements().First(x=>x.Name.LocalName == "Version").Value;
                    IList<Tuple<string, string>> versions;
                    packageVersions.TryGetValue(name, out versions);
                    if (versions == null)
                    {
                        versions = new List<Tuple<string, string>>();
                        packageVersions[name] = versions;
                    }
                    versions.Add(Tuple.Create(version, fileName));
                }
            }
        });

        context.Information("Checking installed NuGet package dependencies versions:");

        packageVersions.ToList().ForEach(package =>
        {
            var packageVersion = package.Value.First().Item1;
            bool isValidVersion = package.Value.All(x => x.Item1 == packageVersion);
            if (!isValidVersion)
            {
                context.Information("Error: package {0} has multiple versions installed:", package.Key);
                foreach (var v in package.Value)
                {
                    context.Information("{0}, file: {1}", v.Item1, v.Item2);
                }
                throw new Exception("Detected multiple NuGet package version installed for different projects.");
            }
        });

        context.Information("Setting NuGet package dependencies versions:");

        var SerilogVersion = packageVersions["Serilog"].FirstOrDefault().Item1;
        var SerilogSinksDebugVersion = packageVersions["Serilog.Sinks.Debug"].FirstOrDefault().Item1;
        var SerilogSinksTraceVersion = packageVersions["Serilog.Sinks.Trace"].FirstOrDefault().Item1;
        var SpracheVersion = packageVersions["Sprache"].FirstOrDefault().Item1;
        var SystemReactiveVersion = packageVersions["System.Reactive"].FirstOrDefault().Item1;
        var ReactiveUIVersion = packageVersions["reactiveui"].FirstOrDefault().Item1;
        var SystemValueTupleVersion = packageVersions["System.ValueTuple"].FirstOrDefault().Item1;
        SkiaSharpVersion = packageVersions["SkiaSharp"].FirstOrDefault().Item1;
		SkiaSharpLinuxVersion = packageVersions["Avalonia.Skia.Linux.Natives"].FirstOrDefault().Item1;
        var SharpDXVersion = packageVersions["SharpDX"].FirstOrDefault().Item1;
        var SharpDXDirect2D1Version = packageVersions["SharpDX.Direct2D1"].FirstOrDefault().Item1;
        var SharpDXDirect3D11Version = packageVersions["SharpDX.Direct3D11"].FirstOrDefault().Item1;
        var SharpDXDirect3D9Version = packageVersions["SharpDX.Direct3D9"].FirstOrDefault().Item1;
        var SharpDXDXGIVersion = packageVersions["SharpDX.DXGI"].FirstOrDefault().Item1;

        context.Information("Package: Serilog, version: {0}", SerilogVersion);
        context.Information("Package: Sprache, version: {0}", SpracheVersion);
        context.Information("Package: System.Reactive, version: {0}", SystemReactiveVersion);
        context.Information("Package: reactiveui, version: {0}", ReactiveUIVersion);
        context.Information("Package: System.ValueTuple, version: {0}", SystemValueTupleVersion);
        context.Information("Package: SkiaSharp, version: {0}", SkiaSharpVersion);
        context.Information("Package: Avalonia.Skia.Linux.Natives, version: {0}", SkiaSharpLinuxVersion);
        context.Information("Package: SharpDX, version: {0}", SharpDXVersion);
        context.Information("Package: SharpDX.Direct2D1, version: {0}", SharpDXDirect2D1Version);
        context.Information("Package: SharpDX.Direct3D11, version: {0}", SharpDXDirect3D11Version);
        context.Information("Package: SharpDX.Direct3D9, version: {0}", SharpDXDirect3D9Version);
        context.Information("Package: SharpDX.DXGI, version: {0}", SharpDXDXGIVersion);

        var nugetPackagesDir = System.Environment.GetEnvironmentVariable("NUGET_HOME")
            ?? System.IO.Path.Combine(System.Environment.GetEnvironmentVariable("USERPROFILE") ?? System.Environment.GetEnvironmentVariable("HOME"), ".nuget");
        
        NugetPackagesDir = System.IO.Path.Combine(nugetPackagesDir, "packages");
        
        var SetNuGetNuspecCommonProperties = new Action<NuGetPackSettings> ((nuspec) => {
            nuspec.Version = parameters.Version;
            nuspec.Authors = new [] { "Avalonia Team" };
            nuspec.Owners = new [] { "stevenk" };
            nuspec.LicenseUrl = new Uri("http://opensource.org/licenses/MIT");
            nuspec.ProjectUrl = new Uri("https://github.com/AvaloniaUI/Avalonia/");
            nuspec.RequireLicenseAcceptance = false;
            nuspec.Symbols = false;
            nuspec.NoPackageAnalysis = true;
            nuspec.Description = "The Avalonia UI framework";
            nuspec.Copyright = "Copyright 2015";
            nuspec.Tags = new [] { "Avalonia" };
        });

        var coreLibraries = new string[][]
        {
            new [] { "./src/", "Avalonia.Animation", ".dll" },
            new [] { "./src/", "Avalonia.Animation", ".xml" },
            new [] { "./src/", "Avalonia.Base", ".dll" },
            new [] { "./src/", "Avalonia.Base", ".xml" },
            new [] { "./src/", "Avalonia.Controls", ".dll" },
            new [] { "./src/", "Avalonia.Controls", ".xml" },
            new [] { "./src/", "Avalonia.DesignerSupport", ".dll" },
            new [] { "./src/", "Avalonia.DesignerSupport", ".xml" },
            new [] { "./src/", "Avalonia.Diagnostics", ".dll" },
            new [] { "./src/", "Avalonia.Diagnostics", ".xml" },
            new [] { "./src/", "Avalonia.Input", ".dll" },
            new [] { "./src/", "Avalonia.Input", ".xml" },
            new [] { "./src/", "Avalonia.Interactivity", ".dll" },
            new [] { "./src/", "Avalonia.Interactivity", ".xml" },
            new [] { "./src/", "Avalonia.Layout", ".dll" },
            new [] { "./src/", "Avalonia.Layout", ".xml" },
            new [] { "./src/", "Avalonia.Logging.Serilog", ".dll" },
            new [] { "./src/", "Avalonia.Logging.Serilog", ".xml" },
            new [] { "./src/", "Avalonia.Visuals", ".dll" },
            new [] { "./src/", "Avalonia.Visuals", ".xml" },
            new [] { "./src/", "Avalonia.Styling", ".dll" },
            new [] { "./src/", "Avalonia.Styling", ".xml" },
            new [] { "./src/", "Avalonia.Themes.Default", ".dll" },
            new [] { "./src/", "Avalonia.Themes.Default", ".xml" },
            new [] { "./src/Markup/", "Avalonia.Markup", ".dll" },
            new [] { "./src/Markup/", "Avalonia.Markup", ".xml" },
            new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".dll" },
            new [] { "./src/Markup/", "Avalonia.Markup.Xaml", ".xml" }
        };

        var coreLibrariesFiles = coreLibraries.Select((lib) => {
            return (FilePath)context.File(lib[0] + lib[1] + "/bin/" + parameters.DirSuffix + "/netstandard2.0/" + lib[1] + lib[2]);
        }).ToList();

        var coreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/netstandard2.0" 
            };
        });

        var win32CoreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/net45" 
            };
        });

        var netcoreappCoreLibrariesNuSpecContent = coreLibrariesFiles.Select((file) => {
            return new NuSpecContent { 
                Source = file.FullPath, Target = "lib/netcoreapp2.0" 
            };
        });

        var net45RuntimePlatformExtensions = new [] {".xml", ".dll"};
        var net45RuntimePlatform = net45RuntimePlatformExtensions.Select(libSuffix => {
            return new NuSpecContent {
                Source = ((FilePath)context.File("./src/Avalonia.DotNetFrameworkRuntime/bin/" + parameters.DirSuffix + "/net461/Avalonia.DotNetFrameworkRuntime" + libSuffix)).FullPath, 
                Target = "lib/net45" 
            };
        });

        var netCoreRuntimePlatformExtensions = new [] {".xml", ".dll"};
        var netCoreRuntimePlatform = netCoreRuntimePlatformExtensions.Select(libSuffix => {
            return new NuSpecContent {
                Source = ((FilePath)context.File("./src/Avalonia.DotNetCoreRuntime/bin/" + parameters.DirSuffix + "/netcoreapp2.0/Avalonia.DotNetCoreRuntime" + libSuffix)).FullPath, 
                Target = "lib/netcoreapp2.0" 
            };
        });

        var toolsContent = new[] {
            new NuSpecContent{
                Source = ((FilePath)context.File("./src/tools/Avalonia.Designer.HostApp/bin/" + parameters.DirSuffix + "/netcoreapp2.0/Avalonia.Designer.HostApp.dll")).FullPath, 
                Target = "tools/netcoreapp2.0/previewer"
            },
            new NuSpecContent{
                Source = ((FilePath)context.File("./src/tools/Avalonia.Designer.HostApp.NetFx/bin/" + parameters.DirSuffix + "/Avalonia.Designer.HostApp.exe")).FullPath, 
                Target = "tools/net461/previewer"
            }
        };

        var nuspecNuGetSettingsCore = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia",
                Dependencies = new DependencyBuilder(this)
                {
                    new NuSpecDependency() { Id = "Serilog", Version = SerilogVersion },
                    new NuSpecDependency() { Id = "Serilog.Sinks.Debug", Version = SerilogSinksDebugVersion },
                    new NuSpecDependency() { Id = "Serilog.Sinks.Trace", Version = SerilogSinksTraceVersion },
                    new NuSpecDependency() { Id = "Sprache", Version = SpracheVersion },
                    new NuSpecDependency() { Id = "System.Reactive", Version = SystemReactiveVersion },
                    new NuSpecDependency() { Id = "Avalonia.Remote.Protocol", Version = parameters.Version },
                    //.NET Core
                    new NuSpecDependency() { Id = "System.Threading.ThreadPool", TargetFramework = "netcoreapp2.0", Version = "4.3.0" },
                    new NuSpecDependency() { Id = "Microsoft.Extensions.DependencyModel", TargetFramework = "netcoreapp2.0", Version = "1.1.0" },
                    new NuSpecDependency() { Id = "NETStandard.Library", TargetFramework = "netcoreapp2.0", Version = "1.6.0" },
                    new NuSpecDependency() { Id = "Serilog", TargetFramework = "netcoreapp2.0", Version = SerilogVersion },
                    new NuSpecDependency() { Id = "Serilog.Sinks.Debug", TargetFramework = "netcoreapp2.0", Version = SerilogSinksDebugVersion },
                    new NuSpecDependency() { Id = "Serilog.Sinks.Trace", TargetFramework = "netcoreapp2.0", Version = SerilogSinksTraceVersion },
                    new NuSpecDependency() { Id = "Sprache", TargetFramework = "netcoreapp2.0", Version = SpracheVersion },
                    new NuSpecDependency() { Id = "System.Reactive", TargetFramework = "netcoreapp2.0", Version = SystemReactiveVersion },
                    new NuSpecDependency() { Id = "Avalonia.Remote.Protocol", TargetFramework = "netcoreapp2.0", Version = parameters.Version },
                }
                .Deps(new string[]{null, "netcoreapp2.0"},
                    "System.ValueTuple", "System.ComponentModel.TypeConverter", "System.ComponentModel.Primitives",
                    "System.Runtime.Serialization.Primitives", "System.Xml.XmlDocument", "System.Xml.ReaderWriter")
                .ToArray(),
                Files = coreLibrariesNuSpecContent
                    .Concat(win32CoreLibrariesNuSpecContent).Concat(net45RuntimePlatform)
                    .Concat(netcoreappCoreLibrariesNuSpecContent).Concat(netCoreRuntimePlatform)
                    .Concat(toolsContent)
                    .ToList(),
                BasePath = context.Directory("./"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.HtmlRenderer
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.HtmlRenderer",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.HtmlRenderer.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Avalonia.HtmlRenderer/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.ReactiveUI
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.ReactiveUI",
                Dependencies = new DependencyBuilder(this)
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                }.Deps(new string[] {null}, "reactiveui"),
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.ReactiveUI.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Avalonia.ReactiveUI/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Remote.Protocol
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Remote.Protocol",
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Remote.Protocol.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Avalonia.Remote.Protocol/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
        };

        var nuspecNuGetSettingsMobile = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Android
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Android",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Android.dll", Target = "lib/MonoAndroid10" }
                },
                BasePath = context.Directory("./src/Android/Avalonia.Android/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.iOS
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.iOS",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.iOS.dll", Target = "lib/Xamarin.iOS10" }
                },
                BasePath = context.Directory("./src/iOS/Avalonia.iOS/bin/" + parameters.DirSuffixIOS),
                OutputDirectory = parameters.NugetRoot
            }
        };

        var nuspecNuGetSettingsDesktop = new []
        {
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Win32
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Win32",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Win32/bin/" + parameters.DirSuffix + "/Avalonia.Win32.dll", Target = "lib/net45" },
                    new NuSpecContent { Source = "Avalonia.Win32.NetStandard/bin/" + parameters.DirSuffix + "/netstandard2.0/Avalonia.Win32.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Windows"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Direct2D1
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Direct2D1",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SharpDX", Version = SharpDXVersion },
                    new NuSpecDependency() { Id = "SharpDX.Direct2D1", Version = SharpDXDirect2D1Version },
                    new NuSpecDependency() { Id = "SharpDX.Direct3D11", Version = SharpDXDirect3D11Version },
                    new NuSpecDependency() { Id = "SharpDX.DXGI", Version = SharpDXDXGIVersion }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Direct2D1.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Windows/Avalonia.Direct2D1/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Gtk3
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Gtk3",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Gtk3.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Gtk/Avalonia.Gtk3/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Skia
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Skia",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion },
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version, TargetFramework="netcoreapp2.0" },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion, TargetFramework="netcoreapp2.0" },
                    new NuSpecDependency() { Id = "Avalonia.Skia.Linux.Natives", Version = SkiaSharpLinuxVersion, TargetFramework="netcoreapp2.0" },
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version, TargetFramework="net461" },
                    new NuSpecDependency() { Id = "SkiaSharp", Version = SkiaSharpVersion, TargetFramework="net461" },
                    new NuSpecDependency() { Id = "Avalonia.Skia.Linux.Natives", Version = SkiaSharpLinuxVersion, TargetFramework="net461" }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Skia.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Skia/Avalonia.Skia/bin/" + parameters.DirSuffix + "/netstandard2.0"),
                OutputDirectory = parameters.NugetRoot
            },
            new NuGetPackSettings()
            {
                Id = "Avalonia.MonoMac",
                Dependencies = new DependencyBuilder(this)
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version }
                }.Dep("MonoMac.NetStandard").ToArray(),
                Files = new []
                {
                    new NuSpecContent { Source = "netstandard2.0/Avalonia.MonoMac.dll", Target = "lib/netstandard2.0" },
                },
                BasePath = context.Directory("./src/OSX/Avalonia.MonoMac/bin/" + parameters.DirSuffix),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.Desktop
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.Desktop",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia.Direct2D1", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Win32", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Gtk3", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.MonoMac", Version = parameters.Version }
                },
                Files = new NuSpecContent[]
                {
                    new NuSpecContent { Source = "licence.md", Target = "" }
                },
                BasePath = context.Directory("./"),
                OutputDirectory = parameters.NugetRoot
            },
            new NuGetPackSettings()
            {
                Id = "Avalonia.Win32.Interoperability",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia.Win32", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Direct2D1", Version = parameters.Version },
                    new NuSpecDependency() { Id = "SharpDX.Direct3D9", Version = SharpDXDirect3D9Version },
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.Win32.Interop/bin/" + parameters.DirSuffix + "/Avalonia.Win32.Interop.dll", Target = "lib/net45" }
                },
                BasePath = context.Directory("./src/Windows"),
                OutputDirectory = parameters.NugetRoot
            },
            ///////////////////////////////////////////////////////////////////////////////
            // Avalonia.LinuxFramebuffer
            ///////////////////////////////////////////////////////////////////////////////
            new NuGetPackSettings()
            {
                Id = "Avalonia.LinuxFramebuffer",
                Dependencies = new []
                {
                    new NuSpecDependency() { Id = "Avalonia", Version = parameters.Version },
                    new NuSpecDependency() { Id = "Avalonia.Skia", Version = parameters.Version }
                },
                Files = new []
                {
                    new NuSpecContent { Source = "Avalonia.LinuxFramebuffer/bin/" + parameters.DirSuffix + "/netstandard2.0/Avalonia.LinuxFramebuffer.dll", Target = "lib/netstandard2.0" }
                },
                BasePath = context.Directory("./src/Linux/"),
                OutputDirectory = parameters.NugetRoot
            }
        };

        NuspecNuGetSettings = new List<NuGetPackSettings>();

        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsCore);
        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsDesktop);
        NuspecNuGetSettings.AddRange(nuspecNuGetSettingsMobile);

        NuspecNuGetSettings.ForEach((nuspec) => SetNuGetNuspecCommonProperties(nuspec));

        NugetPackages = NuspecNuGetSettings.Select(nuspec => {
            return nuspec.OutputDirectory.CombineWithFilePath(string.Concat(nuspec.Id, ".", nuspec.Version, ".nupkg"));
        }).ToArray();

        BinFiles = NuspecNuGetSettings.SelectMany(nuspec => {
            return nuspec.Files.Select(file => {
                return ((DirectoryPath)nuspec.BasePath).CombineWithFilePath(file.Source);
            });
        }).GroupBy(f => f.FullPath).Select(g => g.First()).ToArray();
    }
}
