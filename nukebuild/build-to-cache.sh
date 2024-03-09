#!/bin/sh
dotnet run --project _build.csproj -- --target BuildToNuGetCache --skip CompileHtmlPreviewer Compile Clean
