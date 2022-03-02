#!/usr/bin/env bash

cd $(cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd)
dotnet restore -r osx-arm64
dotnet msbuild -t:BundleApp -p:RuntimeIdentifier=osx-arm64 -p:_AvaloniaUseExternalMSBuild=false