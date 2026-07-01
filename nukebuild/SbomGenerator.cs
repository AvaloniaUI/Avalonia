#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Serilog.Log;

// Generates one CycloneDX SBOM per published NuGet package, using the already-restored
// solution (obj/project.assets.json) so component versions are the ones actually resolved
// into the build, rather than the floating PackageReference ranges GitHub's own dependency
// graph reports.
//
// Numerge (see numerge.json) merges several source projects' packed output into a handful of
// the final NuGet packages (e.g. "Avalonia" absorbs Avalonia.Base, Avalonia.Controls, the
// build tasks/analyzers, etc). That merge happens on the built .nupkg files, not via MSBuild
// ProjectReferences, so it can't be discovered by pointing CycloneDX at a single project with
// -rs/--recursive. Instead this compares the intermediate and final package sets to work out
// which source projects were folded into which final package, generates a BOM per constituent
// project, and unions their components (de-duplicated by purl) into one BOM per final package.
public static class SbomGenerator
{
    const string CycloneDxToolVersion = "6.2.0";

    class NumergeConfigRoot
    {
        public List<NumergePackageGroup> Packages { get; set; } = new();
    }

    class NumergePackageGroup
    {
        public string Id { get; set; } = "";
        public bool MergeAll { get; set; }
        public List<NumergeMergeChild> Merge { get; set; } = new();
    }

    class NumergeMergeChild
    {
        public string Id { get; set; } = "";
    }

    public static void Generate(
        AbsolutePath rootDirectory,
        AbsolutePath nugetRoot,
        AbsolutePath nugetIntermediateRoot,
        AbsolutePath numergeConfigPath,
        AbsolutePath outputDirectory,
        string version)
    {
        outputDirectory.CreateOrCleanDirectory();

        DotNet($"tool update --global CycloneDX --version {CycloneDxToolVersion}");

        var finalPackageIds = nugetRoot.GlobFiles("*.nupkg").Select(p => ReadPackageId((string)p)).ToHashSet();
        var intermediatePackageIds = nugetIntermediateRoot.GlobFiles("*.nupkg")
            .Select(p => ReadPackageId((string)p)).Distinct();

        var numerge = JsonSerializer.Deserialize<NumergeConfigRoot>(File.ReadAllText(numergeConfigPath))
            ?? new NumergeConfigRoot();
        var explicitParentByChild = numerge.Packages
            .SelectMany(p => p.Merge.Select(c => (Parent: p.Id, Child: c.Id)))
            .ToDictionary(x => x.Child, x => x.Parent);
        var mergeAllParent = numerge.Packages.FirstOrDefault(p => p.MergeAll)?.Id;

        // Every final package starts out as its own sole constituent; leftover intermediate
        // packages (ones that never shipped standalone) get assigned to whichever final
        // package absorbed them, per numerge.json.
        var constituentProjectIdsByFinalId = finalPackageIds.ToDictionary(id => id, id => new List<string> { id });
        foreach (var id in intermediatePackageIds.Where(id => !finalPackageIds.Contains(id)))
        {
            var owner = explicitParentByChild.TryGetValue(id, out var explicitOwner) ? explicitOwner : mergeAllParent;
            if (owner is not null && constituentProjectIdsByFinalId.TryGetValue(owner, out var siblings))
                siblings.Add(id);
            else
                Warning($"SBOM: couldn't determine which published package absorbs intermediate package '{id}' - it will be missing from all generated SBOMs.");
        }

        foreach (var (finalId, projectIds) in constituentProjectIdsByFinalId)
            GenerateForPackage(rootDirectory, outputDirectory, version, finalId, projectIds);
    }

    static void GenerateForPackage(AbsolutePath rootDirectory, AbsolutePath outputDirectory, string version,
        string finalId, List<string> projectIds)
    {
        JsonObject? merged = null;
        var seenComponentKeys = new HashSet<string>();
        var scannedProjectDirs = new List<AbsolutePath>();

        foreach (var projectId in projectIds)
        {
            var project = rootDirectory.GlobFiles($"src/**/{projectId}.csproj")
                .Concat(rootDirectory.GlobFiles($"packages/**/{projectId}.csproj"))
                .FirstOrDefault();
            if (project is null)
            {
                Warning($"SBOM: couldn't locate source project for '{projectId}', skipping it in the SBOM for '{finalId}'.");
                continue;
            }
            scannedProjectDirs.Add(project.Parent);

            var tempBom = outputDirectory / $"_{projectId}.tmp.json";
            ProcessTasks.StartProcess("dotnet-CycloneDX",
                    $"\"{project}\" -o \"{outputDirectory}\" -fn \"{tempBom.Name}\" -F Json -dpr -ed -sn \"{finalId}\" -sv \"{version}\"",
                    workingDirectory: rootDirectory)
                .AssertZeroExitCode();

            var doc = JsonNode.Parse(File.ReadAllText(tempBom))!.AsObject();
            File.Delete(tempBom);

            var components = doc["components"]?.AsArray() ?? new JsonArray();
            if (merged is null)
            {
                merged = doc;
                foreach (var component in components)
                    seenComponentKeys.Add(ComponentKey(component));
            }
            else
            {
                var target = merged["components"]?.AsArray() ?? (JsonArray)(merged["components"] = new JsonArray());
                foreach (var component in components)
                {
                    if (seenComponentKeys.Add(ComponentKey(component)))
                        target.Add(component!.DeepClone());
                }

                // Every constituent project's own -sn/-sv override makes its root component (and
                // therefore its dependency-graph "ref") identical to the final package's, so merging
                // by ref correctly unions all constituents' dependsOn edges onto that shared root
                // instead of silently keeping only the first project's edges.
                MergeDependencyGraph(merged, doc["dependencies"]?.AsArray() ?? new JsonArray());
            }
        }

        if (merged is null)
        {
            Warning($"SBOM: no source projects could be scanned for '{finalId}', no SBOM was generated for it.");
            return;
        }

        // cyclonedx-dotnet only sees the MSBuild/NuGet graph. Some projects also bundle a
        // Bun/npm-built webapp directly into their published package (e.g. Avalonia.Browser's
        // staticwebassets, Avalonia.DesignerSupport's embedded previewer) - scan those separately
        // so their shipped JS dependencies aren't silently absent from the SBOM.
        foreach (var projectDir in scannedProjectDirs)
            AddNpmComponents(merged, seenComponentKeys, projectDir);

        File.WriteAllText(outputDirectory / $"{finalId}.{version}.cdx.json",
            merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    static void MergeDependencyGraph(JsonObject target, JsonArray incoming)
    {
        var targetDeps = target["dependencies"]?.AsArray() ?? (JsonArray)(target["dependencies"] = new JsonArray());
        var byRef = targetDeps.OfType<JsonObject>().ToDictionary(d => d["ref"]!.GetValue<string>());

        foreach (var node in incoming.OfType<JsonObject>())
        {
            var nodeRef = node["ref"]!.GetValue<string>();
            var dependsOn = node["dependsOn"]?.AsArray().Select(x => x!.GetValue<string>()) ?? Enumerable.Empty<string>();

            if (!byRef.TryGetValue(nodeRef, out var existing))
            {
                existing = node.DeepClone().AsObject();
                targetDeps.Add(existing);
                byRef[nodeRef] = existing;
            }

            var existingDependsOn = existing["dependsOn"]?.AsArray() ?? (JsonArray)(existing["dependsOn"] = new JsonArray());
            var seen = existingDependsOn.Select(x => x!.GetValue<string>()).ToHashSet();
            foreach (var dep in dependsOn)
                if (seen.Add(dep))
                    existingDependsOn.Add(dep);
        }
    }

    // Scans <projectDir>/**/webapp/package.json for production "dependencies" (deliberately
    // ignoring devDependencies, which never ship) and adds them as components. Resolves each
    // dependency's actually-installed version from node_modules where possible, same rationale
    // as scanning project.assets.json instead of trusting floating NuGet version ranges.
    static void AddNpmComponents(JsonObject merged, HashSet<string> seenComponentKeys, AbsolutePath projectDir)
    {
        foreach (string packageJsonPath in projectDir.GlobFiles("**/webapp/package.json"))
        {
            var packageJson = JsonNode.Parse(File.ReadAllText(packageJsonPath))!.AsObject();
            var nodeModules = ((AbsolutePath)packageJsonPath).Parent / "node_modules";
            var dependencies = packageJson["dependencies"]?.AsObject() ?? new JsonObject();

            foreach (var (name, rangeNode) in dependencies)
            {
                var (purl, componentVersion) = ResolveNpmComponent(name, rangeNode!.GetValue<string>(), nodeModules);
                if (!seenComponentKeys.Add(purl))
                    continue;

                var target = merged["components"]?.AsArray() ?? (JsonArray)(merged["components"] = new JsonArray());
                target.Add(new JsonObject
                {
                    ["type"] = "library",
                    ["name"] = name,
                    ["version"] = componentVersion,
                    ["purl"] = purl
                });
            }
        }
    }

    static (string Purl, string Version) ResolveNpmComponent(string name, string declaredRange, AbsolutePath nodeModules)
    {
        if (declaredRange.StartsWith("github:") || declaredRange.StartsWith("git") || declaredRange.Contains("://"))
        {
            var (owner, repo, reference) = ParseGitDependency(declaredRange);
            return ($"pkg:github/{owner}/{repo}@{reference}", reference);
        }

        var installedPackageJson = nodeModules / name / "package.json";
        if (!File.Exists(installedPackageJson))
        {
            Warning($"SBOM: npm dependency '{name}' isn't installed under {nodeModules} - recording its declared range '{declaredRange}' instead of a resolved version.");
            return ($"pkg:npm/{EncodeNpmName(name)}@{declaredRange}", declaredRange);
        }

        var installedVersion = JsonNode.Parse(File.ReadAllText(installedPackageJson))!["version"]!.GetValue<string>();
        return ($"pkg:npm/{EncodeNpmName(name)}@{installedVersion}", installedVersion);
    }

    static string EncodeNpmName(string name) => name.StartsWith("@") ? $"%40{name[1..]}" : name;

    static (string Owner, string Repo, string Reference) ParseGitDependency(string spec)
    {
        var hashIndex = spec.IndexOf('#');
        var reference = hashIndex >= 0 ? spec[(hashIndex + 1)..] : "HEAD";
        var withoutRef = hashIndex >= 0 ? spec[..hashIndex] : spec;

        var match = Regex.Match(withoutRef, @"github(?:\.com)?[:/]+([^/]+)/([^/#]+?)(?:\.git)?$");
        if (!match.Success)
            throw new NotSupportedException($"SBOM: don't know how to parse git dependency specifier '{spec}'.");
        return (match.Groups[1].Value, match.Groups[2].Value, reference);
    }

    static string ComponentKey(JsonNode? component) =>
        component?["purl"]?.GetValue<string>() ?? component?["name"]?.GetValue<string>() ?? Guid.NewGuid().ToString();

    static string ReadPackageId(string nupkgPath)
    {
        using var file = File.Open(nupkgPath, FileMode.Open, FileAccess.Read);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        var nuspecEntry = zip.Entries.First(e => e.FullName.EndsWith(".nuspec") && e.FullName == e.Name);
        return XDocument.Load(nuspecEntry.Open()).Root!
            .Elements().First(x => x.Name.LocalName == "metadata")
            .Elements().First(x => x.Name.LocalName == "id").Value;
    }
}
