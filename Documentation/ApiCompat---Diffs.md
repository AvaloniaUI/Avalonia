https://github.com/AvaloniaUI/Avalonia/pull/4348 introduced API Compatibility checks.

This means if binary api compatibility is broken it will be a build error.

## Disabling ApiCompat during development

If you want to disable ApiCompat during development, add `<RunApiCompat>false</RunApiCompat>` to `Directory.Build.Props` in the existing `<PropertyGroup>`.

Be sure not to check this change in to the repo!

## Committing an API change

If it is agreed to break api compatibility then the command: `dotnet build /p:BaselineAllAPICompatError=true` can be run.

This will generate an `ApiCompatBaseLine.txt` file next to the projects .csproj file. This describes the apis difference and makes an exception for them.

This file can be checked in to the repo.
