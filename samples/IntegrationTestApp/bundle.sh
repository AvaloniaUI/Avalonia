#!/usr/bin/env bash

cd $(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)

arch="x64"

if [[ $(uname -m) == 'arm64' ]]; then
arch="arm64"
fi

dotnet restore -r osx-$arch
dotnet msbuild -t:BundleApp -p:RuntimeIdentifier=osx-$arch -p:_AvaloniaUseExternalMSBuild=false