using Microsoft.CodeAnalysis.Diagnostics;

namespace Avalonia.Generators;

internal static class GeneratorExtensions
{
    public static string GetMsBuildProperty(
        this AnalyzerConfigOptions options,
        string name,
        string defaultValue = "")
    {
        options.TryGetValue($"build_property.{name}", out var value);
        return value ?? defaultValue;
    }
}
