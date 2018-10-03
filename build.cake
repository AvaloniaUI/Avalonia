#load "./parameters.cake"

Setup<Parameters>(context =>
{
    Information("Running tasks...");
    return new Parameters(context);
});

Teardown<Parameters>((context, parameters) =>
{
    Information("Finished running tasks.");
});

Task("Clean")
    .Does<Parameters>(parameters => 
{
    foreach(var project in parameters.BuildProjects)
    {
        (string path, string name) = project;
        Information($"Clean: {name}");
        DotNetCoreClean($"{path}/{name}/{name}.csproj", new DotNetCoreCleanSettings {
            Configuration = parameters.Configuration,
            Verbosity = DotNetCoreVerbosity.Minimal
        });
    }
});

Task("Build")
    .Does<Parameters>(parameters => 
{
    foreach(var project in parameters.BuildProjects)
    {
        (string path, string name) = project;
        Information($"Build: {name}");
        DotNetCoreBuild($"{path}/{name}/{name}.csproj", new DotNetCoreBuildSettings {
            Configuration = parameters.Configuration,
            VersionSuffix = parameters.VersionSuffix
        });
    }
});

Task("Test")
    .Does<Parameters>(parameters => 
{
    foreach(var project in parameters.TestProjects)
    {
        (string path, string name) = project;
        Information($"Test: {name}");
        DotNetCoreTest($"{path}/{name}/{name}.csproj", new DotNetCoreTestSettings {
            Configuration = parameters.Configuration
        });
    }
});

Task("Publish")
    .Does<Parameters>(parameters => 
{
    CleanDirectory($"{parameters.Artifacts}/zip");
    var redistVersion = "14.15.26706";
    var redistPath = $"C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\VC\\Redist\\MSVC\\{redistVersion}\\x64\\Microsoft.VC141.CRT\\";
    var redistRuntime = "win7-x64";
    foreach(var project in parameters.PublishProjects)
    {
        (string path, string name, string framework, string runtime) = project;
        var output = $"./{parameters.Artifacts}/publish/{name}-{framework}-{runtime}";
        Information($"Publish: {name}, {framework}, {runtime}");
        DotNetCorePublish($"{path}/{name}/{name}.csproj", new DotNetCorePublishSettings {
            Configuration = parameters.Configuration,
            VersionSuffix = parameters.VersionSuffix,
            Framework = framework,
            Runtime = runtime,
            OutputDirectory = output
        });
        if (string.Compare(runtime, redistRuntime, StringComparison.OrdinalIgnoreCase) == 0)
        {
            CopyFileToDirectory($"{redistPath}msvcp140.dll", output);
            CopyFileToDirectory($"{redistPath}vcruntime140.dll",  output);
        }
        Zip($"{parameters.Artifacts}/publish/{name}-{framework}-{runtime}", $"{parameters.Artifacts}/zip/{name}-{framework}-{runtime}.zip");
    }
});

Task("Pack")
    .Does<Parameters>(parameters => 
{
    CleanDirectory($"{parameters.Artifacts}/nuget");
    foreach(var project in parameters.PackProjects)
    {
        (string path, string name) = project;
        Information($"Pack: {name}");
        DotNetCorePack($"{path}/{name}/{name}.csproj", new DotNetCorePackSettings {
            Configuration = parameters.Configuration,
            VersionSuffix = parameters.VersionSuffix,
            OutputDirectory = $"{parameters.Artifacts}/nuget"
        });
    }
});

Task("Push")
    //.WithCriteria<Parameters>((context, parameters) => parameters.PushNuGet)
    .Does<Parameters>(parameters => 
{
    var apiKey = EnvironmentVariable(parameters.IsNugetRelease ? "NUGET_API_KEY" : "MYGET_API_KEY");
    var apiUrl = EnvironmentVariable(parameters.IsNugetRelease ? "NUGET_API_URL" : "MYGET_API_URL");
    var packages = GetFiles($"{parameters.Artifacts}/nuget/*.nupkg");
    foreach (var package in packages)
    {
        DotNetCoreNuGetPush(package.FullPath, new DotNetCoreNuGetPushSettings {
            Source = apiUrl,
            ApiKey = apiKey
        });
    }
});

Task("Default")
  .IsDependentOn("Build");

Task("AppVeyor")
  .IsDependentOn("Clean")
  .IsDependentOn("Build")
  .IsDependentOn("Test")
  .IsDependentOn("Publish")
  .IsDependentOn("Pack")
  .IsDependentOn("Push");

Task("Travis")
  .IsDependentOn("Test");

Task("CircleCI")
  .IsDependentOn("Test");

Task("Azure")
  .IsDependentOn("Clean")
  .IsDependentOn("Build")
  .IsDependentOn("Pack")
  .IsDependentOn("Push");

Task("Azure-macOS")
  .IsDependentOn("Test");

Task("Azure-Linux")
  .IsDependentOn("Test");

RunTarget(Context.Argument("target", "Default"));