#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
BASE_VERSION=$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$SCRIPT_DIR/build/SharedVersion.props")
FORK_VERSION="${1:-11.9.1}"
CONFIGURATION="${2:-Release}"
OUTPUT_DIR="$SCRIPT_DIR/forked_nugets"

echo "============================================"
echo "  Base version:  $BASE_VERSION"
echo "  Fork version:  $FORK_VERSION"
echo "  Output:        $OUTPUT_DIR"
echo "============================================"

rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Step 1: Build Avalonia.Base first (we need the patched DLLs)
echo ">> Building Avalonia.Base ..."
dotnet pack "$SCRIPT_DIR/src/Avalonia.Base/Avalonia.Base.csproj" \
    -c "$CONFIGURATION" \
    -p:ForkVersion="$FORK_VERSION" \
    -o "$OUTPUT_DIR"
echo ""

# Step 2: Repackage the official Avalonia meta-package with our patched Avalonia.Base.dll.
#
# The official Avalonia nupkg bundles Avalonia.Base.dll (via Numerge).
# We replace those DLLs with our patched build and re-version the package
# so consumers get the fix without conflicting copies.
NUGET_CACHE="$HOME/.nuget/packages"
OFFICIAL_NUPKG="$NUGET_CACHE/avalonia/$BASE_VERSION/avalonia.$BASE_VERSION.nupkg"

if [ ! -f "$OFFICIAL_NUPKG" ]; then
    echo ">> Official Avalonia $BASE_VERSION not found in NuGet cache. Downloading..."
    mkdir -p "$NUGET_CACHE/avalonia/$BASE_VERSION"
    curl -sSL -o "$OFFICIAL_NUPKG" \
        "https://api.nuget.org/v3-flatcontainer/avalonia/$BASE_VERSION/avalonia.$BASE_VERSION.nupkg"
    if [ ! -f "$OFFICIAL_NUPKG" ]; then
        echo "   ERROR: Failed to download Avalonia $BASE_VERSION from nuget.org"
        exit 1
    fi
    echo "   Downloaded to $OFFICIAL_NUPKG"
fi

echo ">> Repackaging official Avalonia $BASE_VERSION with patched Avalonia.Base DLLs..."

# Built DLLs from step 1
BASE_BIN="$SCRIPT_DIR/src/Avalonia.Base/bin/$CONFIGURATION"

python3 - "$OFFICIAL_NUPKG" "$BASE_BIN" "$OUTPUT_DIR" "$BASE_VERSION" "$FORK_VERSION" <<'PYEOF'
import sys, os, zipfile, tempfile, shutil
import xml.etree.ElementTree as ET

official_nupkg = sys.argv[1]
base_bin = sys.argv[2]
output_dir = sys.argv[3]
base_version = sys.argv[4]
fork_version = sys.argv[5]

# Map TFM folders in the nupkg to our build output directories
TFM_MAP = {
    "net8.0": "net8.0",
    "net6.0": "net6.0",
    "netstandard2.0": "netstandard2.0",
}

with tempfile.TemporaryDirectory() as tmpdir:
    # Extract official package
    with zipfile.ZipFile(official_nupkg, 'r') as z:
        z.extractall(tmpdir)

    # Replace Avalonia.Base.dll in lib/ and ref/ for each TFM
    for nupkg_tfm, build_tfm in TFM_MAP.items():
        built_dll = os.path.join(base_bin, build_tfm, "Avalonia.Base.dll")
        built_xml = os.path.join(base_bin, build_tfm, "Avalonia.Base.xml")

        if not os.path.exists(built_dll):
            print(f"  WARNING: {built_dll} not found, skipping {nupkg_tfm}")
            continue

        for prefix in ["lib", "ref"]:
            target_dll = os.path.join(tmpdir, prefix, nupkg_tfm, "Avalonia.Base.dll")
            target_xml = os.path.join(tmpdir, prefix, nupkg_tfm, "Avalonia.Base.xml")

            if os.path.exists(target_dll):
                shutil.copy2(built_dll, target_dll)
                print(f"  Replaced {prefix}/{nupkg_tfm}/Avalonia.Base.dll")

            if os.path.exists(target_xml) and os.path.exists(built_xml):
                shutil.copy2(built_xml, target_xml)
                print(f"  Replaced {prefix}/{nupkg_tfm}/Avalonia.Base.xml")

    # Update version in nuspec
    nuspec_files = [f for f in os.listdir(tmpdir) if f.endswith(".nuspec")]
    if nuspec_files:
        nuspec_path = os.path.join(tmpdir, nuspec_files[0])
        tree = ET.parse(nuspec_path)
        root = tree.getroot()

        # Find namespace
        ns = ""
        for uri in [
            "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd",
            "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd",
            "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd",
        ]:
            if root.find(f"{{{uri}}}metadata") is not None:
                ns = uri
                break

        if ns:
            ET.register_namespace("", ns)
            metadata = root.find(f"{{{ns}}}metadata")
            version_el = metadata.find(f"{{{ns}}}version")
            if version_el is not None:
                version_el.text = fork_version
                print(f"  Version: {base_version} -> {fork_version}")

            tree.write(nuspec_path, xml_declaration=True, encoding="utf-8")

    # Remove [Content_Types].xml signature files that NuGet will regenerate
    for f in [".signature.p7s"]:
        p = os.path.join(tmpdir, "package", f)
        if os.path.exists(p):
            os.remove(p)

    # Repackage
    out_nupkg = os.path.join(output_dir, f"Avalonia.{fork_version}.nupkg")
    with zipfile.ZipFile(out_nupkg, 'w', zipfile.ZIP_DEFLATED) as zout:
        for dirpath, dirnames, filenames in os.walk(tmpdir):
            for fn in filenames:
                full = os.path.join(dirpath, fn)
                arcname = os.path.relpath(full, tmpdir)
                zout.write(full, arcname)
    print(f"  Created {os.path.basename(out_nupkg)}")
PYEOF

echo ""

# Remove the standalone Avalonia.Base nupkg — it's now inside the Avalonia meta-package
rm -f "$OUTPUT_DIR"/Avalonia.Base.*.nupkg
echo ">> Removed standalone Avalonia.Base nupkg (now bundled in Avalonia meta-package)"
echo ""

# Step 3: Pack the remaining forked projects
PROJECTS=(
    "src/Avalonia.OpenGL/Avalonia.OpenGL.csproj"
    "src/Skia/Avalonia.Skia/Avalonia.Skia.csproj"
    "src/Android/Avalonia.Android/Avalonia.Android.csproj"
    "src/iOS/Avalonia.iOS/Avalonia.iOS.csproj"
)

for proj in "${PROJECTS[@]}"; do
    echo ">> Packing $proj ..."
    dotnet pack "$SCRIPT_DIR/$proj" \
        -c "$CONFIGURATION" \
        -p:ForkVersion="$FORK_VERSION" \
        -o "$OUTPUT_DIR"
    echo ""
done

# Post-process nuspecs to match official nuget.org package structure.
#
# The official Avalonia build uses Numerge to merge sub-packages into the
# Avalonia meta-package. These sub-packages don't exist on nuget.org:
#   Avalonia.Controls, Avalonia.DesignerSupport, Avalonia.OpenGL,
#   Avalonia.Metal, Avalonia.Vulkan, Avalonia.Dialogs, Avalonia.Markup,
#   Avalonia.Markup.Xaml, Avalonia.MicroCom
#
# Our dotnet pack creates deps on these non-existent packages. We fix this by:
# 1. Removing deps on merged/bundled packages (they're inside the Avalonia meta-package)
# 2. Rewriting Avalonia deps to point to our fork version (since we now ship a forked Avalonia nupkg)
# 3. Removing deps on Avalonia.Base (now bundled in our forked Avalonia meta-package)

echo ">> Post-processing nupkgs..."

python3 - "$OUTPUT_DIR" "$BASE_VERSION" "$FORK_VERSION" <<'PYEOF'
import sys, os, zipfile, tempfile
import xml.etree.ElementTree as ET

output_dir = sys.argv[1]
base_version = sys.argv[2]
fork_version = sys.argv[3]

# Packages bundled inside the Avalonia meta-package (don't exist on nuget.org)
MERGED = {
    "Avalonia.Base", "Avalonia.Controls", "Avalonia.DesignerSupport",
    "Avalonia.OpenGL", "Avalonia.Metal", "Avalonia.Vulkan",
    "Avalonia.Dialogs", "Avalonia.Markup", "Avalonia.Markup.Xaml",
    "Avalonia.MicroCom"
}

# Our forked packages (keep deps on these at fork_version)
FORKED = {"Avalonia.Skia", "Avalonia.OpenGL", "Avalonia.Android", "Avalonia.iOS"}

NS_URIS = [
    "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd",
    "http://schemas.microsoft.com/packaging/2012/06/nuspec.xsd",
    "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd",
]

def find_ns(root):
    for uri in NS_URIS:
        if root.find(f"{{{uri}}}metadata") is not None:
            return uri
    return ""

for fname in sorted(os.listdir(output_dir)):
    if not fname.endswith(".nupkg"):
        continue
    # Skip the repackaged Avalonia meta-package — already handled
    # Its filename is "Avalonia.<fork_version>.nupkg" (no sub-name like .Android, .Skia, etc.)
    pkg_name = fname.rsplit("." + fork_version, 1)[0] if ("." + fork_version) in fname else fname
    if pkg_name == "Avalonia":
        continue
    nupkg_path = os.path.join(output_dir, fname)

    with tempfile.TemporaryDirectory() as tmpdir:
        with zipfile.ZipFile(nupkg_path, 'r') as z:
            z.extractall(tmpdir)

        nuspec_files = [f for f in os.listdir(tmpdir) if f.endswith(".nuspec")]
        if not nuspec_files:
            continue
        nuspec_path = os.path.join(tmpdir, nuspec_files[0])
        tree = ET.parse(nuspec_path)
        root = tree.getroot()
        ns = find_ns(root)
        if not ns:
            continue

        ET.register_namespace("", ns)
        metadata = root.find(f"{{{ns}}}metadata")
        pkg_id = metadata.find(f"{{{ns}}}id").text
        modified = False

        deps_el = metadata.find(f"{{{ns}}}dependencies")
        if deps_el is None:
            continue

        for group in deps_el.findall(f"{{{ns}}}group"):
            to_remove = []
            has_avalonia_dep = False
            needs_avalonia_dep = False

            for dep in group.findall(f"{{{ns}}}dependency"):
                dep_id = dep.get("id")
                dep_ver = dep.get("version", "")

                if dep_id == "Avalonia":
                    has_avalonia_dep = True
                    # Point to our forked Avalonia meta-package
                    if dep_ver != fork_version:
                        dep.set("version", fork_version)
                        modified = True
                        print(f"  {pkg_id}: Avalonia dep {dep_ver} -> {fork_version}")

                elif dep_id in MERGED:
                    # Remove deps on packages bundled in the Avalonia meta-package
                    to_remove.append(dep)
                    needs_avalonia_dep = True

                elif dep_id in FORKED:
                    # Keep fork deps at fork version (already correct)
                    pass

                # All other deps (SkiaSharp, Xamarin, etc.) stay unchanged

            for dep in to_remove:
                group.remove(dep)
                modified = True
                print(f"  {pkg_id}: removed dep on {dep.get('id')}")

            # If we removed merged deps, ensure Avalonia dep exists
            if needs_avalonia_dep and not has_avalonia_dep:
                new_dep = ET.SubElement(group, f"{{{ns}}}dependency")
                new_dep.set("id", "Avalonia")
                new_dep.set("version", fork_version)
                new_dep.set("exclude", "Build,Analyzers")
                modified = True
                print(f"  {pkg_id}: added dep on Avalonia {fork_version}")

        if modified:
            tree.write(nuspec_path, xml_declaration=True, encoding="utf-8")
            os.remove(nupkg_path)
            with zipfile.ZipFile(nupkg_path, 'w', zipfile.ZIP_DEFLATED) as zout:
                for dirpath, dirnames, filenames in os.walk(tmpdir):
                    for fn in filenames:
                        full = os.path.join(dirpath, fn)
                        arcname = os.path.relpath(full, tmpdir)
                        zout.write(full, arcname)
            print(f"  {pkg_id}: repackaged OK")
PYEOF

echo ""
echo "============================================"
echo "  Done! Packages:"
echo "============================================"
ls -1 "$OUTPUT_DIR"/*.nupkg 2>/dev/null
echo ""
echo "Copy to your local feed and clean stale cache:"
echo "  cp $OUTPUT_DIR/*.nupkg /opt/dev/local_nugets/"
echo "  rm -rf ~/.nuget/packages/avalonia/$FORK_VERSION"
echo "  rm -rf ~/.nuget/packages/avalonia.*/$FORK_VERSION"
