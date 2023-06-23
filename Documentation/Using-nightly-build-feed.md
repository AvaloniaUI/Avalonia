# NuGet Package Sources

To access builds from `master` branch, add `https://nuget-feed-nightly.avaloniaui.net/v3/index.json` to your package sources:

![Package sources](https://user-images.githubusercontent.com/47110241/197408094-86181455-3443-411b-bc7c-f71cac9c7012.png)


If you are creating a [`nuget.config`](https://docs.microsoft.com/en-us/nuget/reference/nuget-config-file) file manually, then you can copy and paste this one:

```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>
  <packageSources>
    <!-- optional -->
    <clear />
    <!-- optional -->
    <add key="api.nuget.org" value="https://api.nuget.org/v3/index.json" />
    <!-- nightly feed -->
    <add key="avalonia-nightly" value="https://nuget-feed-nightly.avaloniaui.net/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
```

Or, you can use `RestoreSources` from `PropertyGroup`, for example, in [`Directory.Build.props`](https://docs.microsoft.com/en-us/visualstudio/msbuild/customize-your-build) like this:
```xml
<Project>
  <PropertyGroup>
    <RestoreSources>
      https://nuget-feed-nightly.avaloniaui.net/v3/index.json;
    </RestoreSources>
  </PropertyGroup>
</Project>
```

# Update Installed Packages

Update your package using Avalonia feed:

![Update](https://user-images.githubusercontent.com/47110241/197407933-33f65a85-4fe1-45fc-b44c-2b5efbcfdb9e.png)


# All builds feed

*Every* build (even from PRs and random branches) is getting published to `https://nuget-feed-all.avaloniaui.net/v3/index.json`

To get the version for a particular build you need to check the build number from the build on Azure Pipelines. Then you can use this build to determine PR package version (or just see the logs on Azure).

**This feed contains packages with *UNTRUSTED* source code** (basically anyone can create a PR a trigger a build), some make sure to actually read the diff of the corresponding pull request. NuGet packages can contain malicious code even in build-time scripts.

