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

# Pack order: OpenGL/Skia first (no fork-to-fork deps), then Android/iOS
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
#   Avalonia.Base, Avalonia.Controls, Avalonia.DesignerSupport, Avalonia.OpenGL,
#   Avalonia.Metal, Avalonia.Vulkan, Avalonia.Dialogs, Avalonia.Markup,
#   Avalonia.Markup.Xaml, Avalonia.MicroCom
#
# Our dotnet pack creates deps on these non-existent packages. We fix this by:
# 1. Removing deps on merged/bundled packages
# 2. Ensuring a dep on Avalonia >= base_version exists (it provides those DLLs)
# 3. Pinning Avalonia dep to base_version (not fork_version)

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
                    # Pin to base version (official meta-package on nuget.org)
                    if dep_ver == fork_version:
                        dep.set("version", base_version)
                        modified = True
                        print(f"  {pkg_id}: Avalonia dep {fork_version} -> {base_version}")

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
                new_dep.set("version", base_version)
                new_dep.set("exclude", "Build,Analyzers")
                modified = True
                print(f"  {pkg_id}: added dep on Avalonia {base_version}")

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
echo "  rm -rf ~/.nuget/packages/avalonia*/11.9.1"
