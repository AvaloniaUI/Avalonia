#!/usr/bin/env bash
set -euo pipefail
sample_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_dir="$(cd "$sample_dir/../.." && pwd)"
output_dir="$repo_dir/artifacts/ubuntu-touch"
mkdir -p "$output_dir"
stage_dir="$(mktemp -d "$output_dir/stage.XXXXXX")"
trap 'rm -rf -- "$stage_dir"' EXIT
mkdir -p "$stage_dir/app"
dotnet publish "$sample_dir/UbuntuTouchDemo.csproj" -c Release -r linux-arm64 --self-contained true \
  -p:PublishTrimmed=false -p:UseSharedCompilation=false -m:1 -nodeReuse:false -o "$stage_dir/app"
cp "$sample_dir"/packaging/* "$stage_dir/"
chmod +x "$stage_dir/launch.sh" "$stage_dir/app/UbuntuTouchDemo"
desktop-file-validate "$stage_dir/inputdemo.desktop"
docker run --rm --user "$(id -u):$(id -g)" \
  --mount "type=bind,src=$output_dir,dst=/work" --workdir /work \
  --entrypoint click "${CLICK_BUILD_IMAGE:-clickable/amd64-ut24.04-2.x-arm64:latest}" build --no-validate "${stage_dir##*/}"
package_name="$(python3 -c 'import json,sys; m=json.load(open(sys.argv[1])); print("{name}_{version}_{architecture}.click".format(**m))' "$sample_dir/packaging/manifest.json")"
sha256sum "$output_dir/$package_name"
