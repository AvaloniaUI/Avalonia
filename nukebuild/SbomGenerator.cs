#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
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
    public static void Generate(
        Tool cycloneDx,
        AbsolutePath rootDirectory,
        AbsolutePath nugetRoot,
        AbsolutePath nugetIntermediateRoot,
        AbsolutePath numergeConfigPath,
        AbsolutePath outputDirectory,
        string version)
    {
        outputDirectory.CreateOrCleanDirectory();

        // Read each final package's id exactly once here (opening/unzipping a nupkg to parse its
        // nuspec isn't free) and reuse the id->path map when locating each package's own nupkg
        // below, rather than re-scanning every nupkg per final package.
        var finalPackagePathsById = nugetRoot.GlobFiles("*.nupkg")
            .GroupBy(p => ReadPackageId((string)p))
            .ToDictionary(g => g.Key, g => g.First());
        var finalPackageIds = finalPackagePathsById.Keys.ToHashSet();
        var intermediatePackageIds = nugetIntermediateRoot.GlobFiles("*.nupkg")
            .Select(p => ReadPackageId((string)p)).Distinct();

        var numerge = Numerge.MergeConfiguration.LoadFile(numergeConfigPath);
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
            GenerateForPackage(cycloneDx, rootDirectory, finalPackagePathsById, outputDirectory, version, finalId, projectIds);
    }

    static void GenerateForPackage(Tool cycloneDx, AbsolutePath rootDirectory,
        IReadOnlyDictionary<string, AbsolutePath> finalPackagePathsById,
        AbsolutePath outputDirectory, string version, string finalId, List<string> projectIds)
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
            cycloneDx(
                $"\"{project}\" -o \"{outputDirectory}\" -fn \"{tempBom.Name}\" -F Json -dpr -ed -sn \"{finalId}\" -sv \"{version}\"",
                workingDirectory: rootDirectory);

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
        var rootRef = merged["metadata"]?["component"]?["bom-ref"]?.GetValue<string>();
        foreach (var projectDir in scannedProjectDirs)
            AddNpmComponents(merged, seenComponentKeys, projectDir, rootRef);

        // The final .nupkg carries the authoritative publisher/license/repository metadata and the
        // actual shipped binaries; use it to flesh out the thin root component cyclonedx-dotnet
        // emits and to verify nothing ships that the dependency scan didn't already account for.
        var finalNupkg = finalPackagePathsById.GetValueOrDefault(finalId);
        if (finalNupkg is not null)
        {
            var nuspec = ReadNuspecMetadata((string)finalNupkg);
            EnrichRootComponent(merged, nuspec);
            AddPackageContentComponents(merged, seenComponentKeys, (string)finalNupkg, finalId, projectIds, nuspec);
        }
        else
        {
            Warning($"SBOM: couldn't find the built .nupkg for '{finalId}' - root metadata and package-content verification were skipped.");
        }

        // cyclonedx-dotnet leaves dependsOn edges pointing at packages it excluded as dev
        // dependencies (e.g. analyzers stripped by -ed), which dangle once the component is gone
        // (upstream bug CycloneDX/cyclonedx-dotnet#761, still reproducing in 6.2.0). Drop those so
        // the graph only references components actually present in the SBOM.
        PruneDanglingDependencyEdges(merged);

        var sbomJson = merged.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputDirectory / $"{finalId}.{version}.cdx.json", sbomJson);

        // Embed the SBOM inside the shipped .nupkg so it travels with the package, in addition to
        // the standalone copy written above (which CI publishes as the SBOM artifact) - belt and
        // suspenders: consumers who only ever see the package still get its bill of materials.
        if (finalNupkg is not null)
            EmbedSbomInPackage(finalNupkg, sbomJson);
    }

    // The path inside the .nupkg where the CycloneDX SBOM is embedded. Mirrors the _manifest/
    // layout Microsoft.Sbom.Targets uses for its SPDX manifest, but keeps CycloneDX's recognised
    // *.cdx.json filename so tools that scan for that pattern still find it once unpacked.
    const string EmbeddedSbomEntryPath = "_manifest/cyclonedx/bom.cdx.json";

    // Adds the generated SBOM as a new part inside the shipped package. Must run before the package
    // is signed - a NuGet signature covers the whole archive, so adding a part afterwards would
    // invalidate it. That holds here: these packages are signed server-side by nuget.org on push,
    // which happens after this build step.
    static void EmbedSbomInPackage(AbsolutePath nupkgPath, string sbomJson)
    {
        using var file = File.Open(nupkgPath, FileMode.Open, FileAccess.ReadWrite);
        using var zip = new ZipArchive(file, ZipArchiveMode.Update);

        if (zip.Entries.Any(e => e.FullName.EndsWith(".signature.p7s", StringComparison.OrdinalIgnoreCase)))
        {
            Warning($"SBOM: '{nupkgPath.Name}' is already signed - skipping embed so its signature stays valid.");
            return;
        }

        // Re-embedding (e.g. a re-run over the same output) should replace, not stack duplicates.
        zip.GetEntry(EmbeddedSbomEntryPath)?.Delete();
        using (var entryStream = zip.CreateEntry(EmbeddedSbomEntryPath).Open())
        using (var writer = new StreamWriter(entryStream))
            writer.Write(sbomJson);

        EnsureJsonContentTypeRegistered(zip);
    }

    // A .nupkg is an OPC package: every part's extension must be declared in [Content_Types].xml or
    // strict OPC readers - including NuGet's own signature verification - reject the package. The
    // SBOM is a .json part, so register that extension before (or as) we add it.
    static void EnsureJsonContentTypeRegistered(ZipArchive zip)
    {
        const string contentTypesEntryName = "[Content_Types].xml";
        XNamespace ns = "http://schemas.openxmlformats.org/package/2006/content-types";

        var entry = zip.GetEntry(contentTypesEntryName);
        if (entry is null)
            return; // not a well-formed OPC package; don't fabricate one

        XDocument doc;
        using (var read = entry.Open())
            doc = XDocument.Load(read);

        var alreadyRegistered = doc.Root!.Elements(ns + "Default")
            .Any(d => string.Equals((string?)d.Attribute("Extension"), "json", StringComparison.OrdinalIgnoreCase));
        if (alreadyRegistered)
            return;

        doc.Root.Add(new XElement(ns + "Default",
            new XAttribute("Extension", "json"),
            new XAttribute("ContentType", "application/json")));

        entry.Delete();
        using var write = zip.CreateEntry(contentTypesEntryName).Open();
        doc.Save(write);
    }

    static void PruneDanglingDependencyEdges(JsonObject merged)
    {
        var deps = merged["dependencies"]?.AsArray();
        if (deps is null)
            return;

        var known = new HashSet<string>();
        var rootRef = merged["metadata"]?["component"]?["bom-ref"]?.GetValue<string>();
        if (rootRef is not null)
            known.Add(rootRef);
        foreach (var component in merged["components"]?.AsArray() ?? new JsonArray())
        {
            if (component?["bom-ref"]?.GetValue<string>() is { } bomRef)
                known.Add(bomRef);
            if (component?["purl"]?.GetValue<string>() is { } purl)
                known.Add(purl);
        }

        foreach (var node in deps.OfType<JsonObject>())
        {
            var dependsOn = node["dependsOn"]?.AsArray();
            if (dependsOn is null)
                continue;
            var kept = new JsonArray();
            foreach (var edge in dependsOn)
                if (known.Contains(edge!.GetValue<string>()))
                    kept.Add(edge.GetValue<string>());
            node["dependsOn"] = kept;
        }
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
    // ignoring devDependencies, which never ship) and adds them - plus their transitive
    // dependencies, walked through the installed node_modules - as fully-formed components:
    // bom-ref, resolved version, license, and dependency-graph edges, matching the shape of the
    // NuGet components cyclonedx-dotnet emits so npm packages aren't second-class SBOM entries.
    // Versions come from the actually-installed node_modules (same rationale as reading
    // project.assets.json rather than trusting floating ranges).
    static void AddNpmComponents(JsonObject merged, HashSet<string> seenComponentKeys, AbsolutePath projectDir,
        string? rootRef)
    {
        foreach (string packageJsonPath in projectDir.GlobFiles("**/webapp/package.json"))
        {
            var packageJson = JsonNode.Parse(File.ReadAllText(packageJsonPath))!.AsObject();
            var nodeModules = ((AbsolutePath)packageJsonPath).Parent / "node_modules";
            var dependencies = packageJson["dependencies"]?.AsObject() ?? new JsonObject();

            // The webapp is bundled into the shipped package, so its direct production
            // dependencies are direct dependencies of the final NuGet package.
            foreach (var (name, rangeNode) in dependencies)
            {
                if (IsTypeOnlyPackage(name))
                    continue;
                var purl = AddNpmComponentTree(merged, seenComponentKeys, nodeModules, name,
                    rangeNode!.GetValue<string>(), nodeModules);
                if (rootRef is not null)
                    AddDependsOn(merged, rootRef, purl);
            }
        }
    }

    // Adds the component for (name, range) and, recursively, everything it depends on, returning
    // its purl. The component/graph node/subtree are materialised only the first time a purl is
    // seen (which also breaks any dependency cycles); repeat encounters just return the purl so
    // the caller can still record its own edge to it.
    static string AddNpmComponentTree(JsonObject merged, HashSet<string> seenComponentKeys,
        AbsolutePath topLevelNodeModules, string name, string declaredRange, AbsolutePath parentNodeModules)
    {
        var (purl, componentVersion, installedDir) =
            ResolveNpmComponent(name, declaredRange, parentNodeModules, topLevelNodeModules);

        if (!seenComponentKeys.Add(purl))
            return purl;

        var installed = installedDir is not null && File.Exists(installedDir / "package.json")
            ? JsonNode.Parse(File.ReadAllText(installedDir / "package.json"))!.AsObject()
            : null;

        var component = new JsonObject
        {
            ["type"] = "library",
            ["bom-ref"] = purl,
            ["name"] = name,
            ["version"] = componentVersion,
            ["purl"] = purl
        };
        // No hashes: npm's verifiable hashes live in the (binary bun) lockfile, not the installed
        // tree, and a hash of the unpacked directory wouldn't be checkable against a registry.
        var licenses = installed is null ? null : BuildLicenses(installed);
        if (licenses is not null)
            component["licenses"] = licenses;

        var target = merged["components"]?.AsArray() ?? (JsonArray)(merged["components"] = new JsonArray());
        target.Add(component);

        var node = new JsonObject { ["ref"] = purl, ["dependsOn"] = new JsonArray() };
        (merged["dependencies"]?.AsArray() ?? (JsonArray)(merged["dependencies"] = new JsonArray())).Add(node);

        var childDeps = installed?["dependencies"]?.AsObject() ?? new JsonObject();
        var childNodeModules = installedDir is not null ? installedDir / "node_modules" : parentNodeModules;
        var dependsOn = node["dependsOn"]!.AsArray();
        foreach (var (childName, childRange) in childDeps)
        {
            if (IsTypeOnlyPackage(childName))
                continue;
            var childPurl = AddNpmComponentTree(merged, seenComponentKeys, topLevelNodeModules, childName,
                childRange!.GetValue<string>(), childNodeModules);
            dependsOn.Add(childPurl);
        }

        return purl;
    }

    // @types/* packages are TypeScript declaration stubs: esbuild strips them at build time, so
    // they're never part of the shipped bytes and don't belong in a scope-of-delivery SBOM.
    static bool IsTypeOnlyPackage(string name) => name.StartsWith("@types/", StringComparison.Ordinal);

    static void AddDependsOn(JsonObject merged, string fromRef, string toPurl)
    {
        var deps = merged["dependencies"]?.AsArray() ?? (JsonArray)(merged["dependencies"] = new JsonArray());
        var node = deps.OfType<JsonObject>().FirstOrDefault(d => d["ref"]?.GetValue<string>() == fromRef);
        if (node is null)
        {
            node = new JsonObject { ["ref"] = fromRef, ["dependsOn"] = new JsonArray() };
            deps.Add(node);
        }

        var dependsOn = node["dependsOn"]?.AsArray() ?? (JsonArray)(node["dependsOn"] = new JsonArray());
        if (!dependsOn.Any(x => x!.GetValue<string>() == toPurl))
            dependsOn.Add(toPurl);
    }

    static JsonArray? BuildLicenses(JsonObject installedPackageJson)
    {
        // Modern npm: "license" is an SPDX id or expression. Legacy: "license"/"licenses" objects.
        if (installedPackageJson["license"] is JsonValue licenseValue && licenseValue.TryGetValue(out string? spdx))
        {
            var licenses = SpdxToLicenses(spdx);
            if (licenses is not null)
                return licenses;
        }

        var legacy = (installedPackageJson["license"] as JsonObject)?["type"]?.GetValue<string>()
            ?? (installedPackageJson["licenses"] as JsonArray)?.OfType<JsonObject>()
                .FirstOrDefault()?["type"]?.GetValue<string>();
        return legacy is null
            ? null
            : new JsonArray(new JsonObject { ["license"] = new JsonObject { ["name"] = legacy } });
    }

    // Turns an SPDX string into a CycloneDX licenses array: a single license id becomes a
    // {license:{id}} entry, a compound SPDX expression becomes an {expression} entry.
    static JsonArray? SpdxToLicenses(string? spdx)
    {
        if (string.IsNullOrWhiteSpace(spdx))
            return null;
        var isExpression = spdx.IndexOf(" OR ", StringComparison.Ordinal) >= 0
            || spdx.IndexOf(" AND ", StringComparison.Ordinal) >= 0
            || spdx.IndexOf(" WITH ", StringComparison.Ordinal) >= 0;
        return new JsonArray(isExpression
            ? new JsonObject { ["expression"] = spdx }
            : new JsonObject { ["license"] = new JsonObject { ["id"] = spdx } });
    }

    static (string Purl, string Version, AbsolutePath? InstalledDir) ResolveNpmComponent(
        string name, string declaredRange, AbsolutePath parentNodeModules, AbsolutePath topLevelNodeModules)
    {
        // npm/bun hoist most packages to the top level but may nest a conflicting version under
        // the depending package, so prefer the nested copy and fall back to the hoisted one.
        var installedDir = new[] { parentNodeModules / name, topLevelNodeModules / name }
            .FirstOrDefault(d => File.Exists(d / "package.json"));

        if (declaredRange.StartsWith("github:") || declaredRange.StartsWith("git") || declaredRange.Contains("://"))
        {
            if (TryParseGitHubDependency(declaredRange, out var owner, out var repo, out var reference))
                return ($"pkg:github/{owner}/{repo}@{reference}", reference, installedDir);

            // A non-GitHub git/URL specifier (GitLab/Bitbucket, a raw tarball URL, ...). We have no
            // provider-specific purl for it, so degrade to a generic component - preferring the
            // version installed on disk - rather than throwing and failing the whole release over a
            // single dependency we can't classify precisely.
            var resolvedVersion = installedDir is not null
                ? JsonNode.Parse(File.ReadAllText(installedDir / "package.json"))!["version"]?.GetValue<string>()
                : null;
            resolvedVersion ??= declaredRange;
            Warning($"SBOM: npm dependency '{name}' uses an unrecognised git/URL specifier '{declaredRange}' - recording it as a generic component with version '{resolvedVersion}'.");
            return ($"pkg:generic/{EncodeNpmName(name)}@{resolvedVersion}", resolvedVersion, installedDir);
        }

        if (installedDir is null)
        {
            Warning($"SBOM: npm dependency '{name}' isn't installed near {parentNodeModules} - recording its declared range '{declaredRange}' instead of a resolved version.");
            return ($"pkg:npm/{EncodeNpmName(name)}@{declaredRange}", declaredRange, null);
        }

        var installedVersion = JsonNode.Parse(File.ReadAllText(installedDir / "package.json"))!["version"]?.GetValue<string>();
        if (installedVersion is null)
        {
            // package.json without a "version" is valid for private packages; don't let it NRE.
            Warning($"SBOM: npm dependency '{name}' installed at {installedDir} has no version in its package.json - recording its declared range '{declaredRange}' instead.");
            installedVersion = declaredRange;
        }
        return ($"pkg:npm/{EncodeNpmName(name)}@{installedVersion}", installedVersion, installedDir);
    }

    static string EncodeNpmName(string name) => name.StartsWith("@") ? $"%40{name[1..]}" : name;

    static bool TryParseGitHubDependency(string spec, out string owner, out string repo, out string reference)
    {
        owner = repo = "";
        var hashIndex = spec.IndexOf('#');
        reference = hashIndex >= 0 ? spec[(hashIndex + 1)..] : "HEAD";
        var withoutRef = hashIndex >= 0 ? spec[..hashIndex] : spec;

        var match = Regex.Match(withoutRef, @"github(?:\.com)?[:/]+([^/]+)/([^/#]+?)(?:\.git)?$");
        if (!match.Success)
            return false;
        owner = match.Groups[1].Value;
        repo = match.Groups[2].Value;
        return true;
    }

    static string ComponentKey(JsonNode? component) =>
        component?["purl"]?.GetValue<string>() ?? component?["name"]?.GetValue<string>() ?? Guid.NewGuid().ToString();

    // Fills in the root component with the publisher, licence, description and repository details
    // from the shipped .nuspec - cyclonedx-dotnet only emits type/name/version, which is far short
    // of the manufacturer/provenance information a CRA-facing SBOM is expected to carry.
    static void EnrichRootComponent(JsonObject merged, NuspecMetadata meta)
    {
        var component = merged["metadata"]?["component"]?.AsObject();
        if (component is null)
            return;

        // These are shipped libraries, not applications (cyclonedx-dotnet's default type).
        component["type"] = "library";
        component["purl"] = $"pkg:nuget/{meta.Id}@{meta.Version}";
        if (meta.Description is not null)
            component["description"] = meta.Description;
        if (meta.Copyright is not null)
            component["copyright"] = meta.Copyright;

        JsonObject? supplier = null;
        if (meta.Authors is not null)
        {
            component["publisher"] = meta.Authors;
            component["author"] = meta.Authors;
            supplier = new JsonObject { ["name"] = meta.Authors };
            if (meta.ProjectUrl is not null)
                supplier["url"] = new JsonArray(meta.ProjectUrl);
            component["supplier"] = supplier;
        }

        var licenses = SpdxToLicenses(meta.LicenseExpression ?? meta.LicenseId);
        // A file license has no SPDX id; record it by name rather than emitting an invalid id.
        if (licenses is null && meta.LicenseFile is not null)
            licenses = new JsonArray(new JsonObject
                { ["license"] = new JsonObject { ["name"] = Path.GetFileName(meta.LicenseFile) } });
        if (licenses is not null)
            component["licenses"] = licenses;

        var externalReferences = new JsonArray();
        if (meta.ProjectUrl is not null)
            externalReferences.Add(new JsonObject { ["url"] = meta.ProjectUrl, ["type"] = "website" });
        if (meta.RepositoryUrl is not null)
            externalReferences.Add(new JsonObject { ["url"] = meta.RepositoryUrl, ["type"] = "vcs" });
        if (externalReferences.Count > 0)
            component["externalReferences"] = externalReferences;

        // Also record the manufacturer at the document level (the entity that supplied the BOM).
        if (supplier is not null && merged["metadata"] is JsonObject metadata)
            metadata["supplier"] = supplier.DeepClone();
    }

    // Cross-checks what the package actually ships against the components derived from the
    // dependency graph. Avalonia's own merged modules (Numerge folds several projects' assemblies
    // into one package) are recorded as manufacturer-supplied components with a verifiable SHA-512
    // of the shipped bytes; any third-party binary that no restored dependency accounts for is
    // added and flagged, so a future bundling regression can't silently escape the SBOM.
    static void AddPackageContentComponents(JsonObject merged, HashSet<string> seenComponentKeys,
        string nupkgPath, string finalId, List<string> constituentProjectIds, NuspecMetadata meta)
    {
        var productNames = new HashSet<string>(constituentProjectIds, StringComparer.OrdinalIgnoreCase) { finalId };
        var representedNames = (merged["components"]?.AsArray() ?? new JsonArray())
            .Select(c => c?["name"]?.GetValue<string>())
            .Where(n => n is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

        var supplier = meta.Authors is null ? null : new JsonObject { ["name"] = meta.Authors };
        var target = merged["components"]?.AsArray() ?? (JsonArray)(merged["components"] = new JsonArray());
        var rootRef = merged["metadata"]?["component"]?["bom-ref"]?.GetValue<string>();

        using var file = File.Open(nupkgPath, FileMode.Open, FileAccess.Read);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries)
        {
            var path = entry.FullName;
            // Reference assemblies under ref/ are compile-time surface, not shipped runtime code;
            // the real implementation lives under lib/ and is scanned there.
            if (!IsShippedBinary(path) || path.StartsWith("ref/", StringComparison.OrdinalIgnoreCase))
                continue;

            var bytes = ReadEntry(entry);
            var assemblyName = TryReadAssemblyName(bytes, out var assemblyVersion);
            var simpleName = assemblyName ?? Path.GetFileNameWithoutExtension(path);

            // Third-party binaries already represented by a NuGet/npm component need no duplicate.
            if (assemblyName is not null && representedNames.Contains(simpleName))
                continue;

            var isProduct = productNames.Contains(simpleName)
                || simpleName.StartsWith("Avalonia.", StringComparison.OrdinalIgnoreCase)
                || simpleName.Equals("Avalonia", StringComparison.OrdinalIgnoreCase);

            // The package's primary assembly is the root component itself - don't list it as its own subcomponent.
            if (isProduct && simpleName.Equals(finalId, StringComparison.OrdinalIgnoreCase))
                continue;

            var version = assemblyVersion ?? meta.Version;
            var bomRef = $"binary:{simpleName}@{version}";
            if (!seenComponentKeys.Add(bomRef))
                continue;

            if (!isProduct)
                Warning($"SBOM: package '{finalId}' ships '{path}' ({simpleName}) which no restored dependency accounts for - added from package contents, please verify its provenance.");

            var component = new JsonObject
            {
                ["type"] = "library",
                ["bom-ref"] = bomRef,
                ["name"] = simpleName,
                ["version"] = version,
                ["scope"] = "required",
                ["hashes"] = new JsonArray(new JsonObject
                {
                    ["alg"] = "SHA-512",
                    ["content"] = Convert.ToHexString(SHA512.HashData(bytes))
                }),
                ["properties"] = new JsonArray(new JsonObject
                {
                    ["name"] = "avalonia:packagePath",
                    ["value"] = path
                })
            };
            if (isProduct && supplier is not null)
                component["supplier"] = supplier.DeepClone();
            target.Add(component);

            // Link the shipped binary into the dependency graph so consumers that traverse from
            // the root component reach it: give it its own (leaf) node and an edge from the root.
            (merged["dependencies"]?.AsArray() ?? (JsonArray)(merged["dependencies"] = new JsonArray()))
                .Add(new JsonObject { ["ref"] = bomRef, ["dependsOn"] = new JsonArray() });
            if (rootRef is not null)
                AddDependsOn(merged, rootRef, bomRef);
        }
    }

    static bool IsShippedBinary(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext is ".dll" or ".so" or ".dylib" or ".wasm" or ".node" or ".a";
    }

    static byte[] ReadEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // Returns the managed assembly's simple name (and version), or null for native / non-managed binaries.
    static string? TryReadAssemblyName(byte[] bytes, out string? version)
    {
        version = null;
        try
        {
            using var pe = new PEReader(new MemoryStream(bytes));
            if (!pe.HasMetadata)
                return null;
            var reader = pe.GetMetadataReader();
            if (!reader.IsAssembly)
                return null;
            var assembly = reader.GetAssemblyDefinition();
            version = assembly.Version.ToString();
            return reader.GetString(assembly.Name);
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    class NuspecMetadata
    {
        public string Id = "";
        public string Version = "";
        public string? Authors;
        public string? LicenseId;
        public string? LicenseExpression;
        public string? LicenseFile;
        public string? ProjectUrl;
        public string? RepositoryUrl;
        public string? Description;
        public string? Copyright;
    }

    static NuspecMetadata ReadNuspecMetadata(string nupkgPath)
    {
        using var file = File.Open(nupkgPath, FileMode.Open, FileAccess.Read);
        using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        var nuspecEntry = zip.Entries.First(e => e.FullName.EndsWith(".nuspec") && e.FullName == e.Name);
        var metadata = XDocument.Load(nuspecEntry.Open()).Root!
            .Elements().First(x => x.Name.LocalName == "metadata");

        string? Value(string name) => metadata.Elements().FirstOrDefault(x => x.Name.LocalName == name)?.Value;
        var license = metadata.Elements().FirstOrDefault(x => x.Name.LocalName == "license");
        var repository = metadata.Elements().FirstOrDefault(x => x.Name.LocalName == "repository");

        return new NuspecMetadata
        {
            Id = Value("id") ?? "",
            Version = Value("version") ?? "",
            Authors = Value("authors"),
            // A nuspec <license> is either type="expression" (an SPDX expression) or type="file"
            // (a path to a bundled licence file); only the former is a valid SPDX id/expression.
            LicenseExpression = license?.Attribute("type")?.Value == "expression" ? license.Value : null,
            LicenseFile = license?.Attribute("type")?.Value == "file" ? license.Value : null,
            LicenseId = license?.Attribute("type")?.Value is "expression" or "file" ? null : license?.Value,
            ProjectUrl = Value("projectUrl"),
            RepositoryUrl = repository?.Attribute("url")?.Value,
            Description = Value("description"),
            Copyright = Value("copyright")
        };
    }

    public static string ReadPackageId(string nupkgPath) => ReadNuspecMetadata(nupkgPath).Id;
}
