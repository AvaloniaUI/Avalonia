# Avalonia Fork

This is a forked version of Avalonia with custom modifications.

## Building

Run the build script with the desired version number:

```bash
./build-fork-nugets.sh <version>
```

If no version is specified, it defaults to `11.9.1`. You can also pass a build configuration as the second argument (defaults to `Release`).

## Output

The build produces the following NuGet packages in `forked_nugets/`:

- `Avalonia.Android.<version>.nupkg`
- `Avalonia.iOS.<version>.nupkg`
- `Avalonia.OpenGL.<version>.nupkg`
- `Avalonia.Skia.<version>.nupkg`
