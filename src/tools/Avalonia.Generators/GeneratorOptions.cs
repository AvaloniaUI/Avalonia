using System;
using Avalonia.Generators.Common;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.NameGenerator;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Avalonia.Generators;

// When update these enum values, don't forget to update Avalonia.Generators.props.
internal enum BuildProperties
{
    AvaloniaNameGeneratorIsEnabled = 0,
    AvaloniaNameGeneratorBehavior = 1,
    AvaloniaNameGeneratorDefaultFieldModifier = 2,
    AvaloniaNameGeneratorFilterByPath = 3,
    AvaloniaNameGeneratorFilterByNamespace = 4,
    AvaloniaNameGeneratorViewFileNamingStrategy = 5,
    AvaloniaNameGeneratorAttachDevTools = 6,
    // TODO add other generators properties here.
}

internal record GeneratorOptions
{
    public GeneratorOptions(AnalyzerConfigOptions options)
    {
        AvaloniaNameGeneratorIsEnabled = GetBoolProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorIsEnabled,
            true);
        AvaloniaNameGeneratorBehavior = GetEnumProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorBehavior,
            Behavior.InitializeComponent);
        AvaloniaNameGeneratorClassFieldModifier = GetEnumProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorDefaultFieldModifier,
            NamedFieldModifier.Internal);
        AvaloniaNameGeneratorViewFileNamingStrategy = GetEnumProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorViewFileNamingStrategy,
            ViewFileNamingStrategy.NamespaceAndClassName);
        AvaloniaNameGeneratorFilterByPath = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorFilterByPath,
            "*"));
        AvaloniaNameGeneratorFilterByNamespace = new GlobPatternGroup(GetStringArrayProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorFilterByNamespace,
            "*"));
        AvaloniaNameGeneratorAttachDevTools = GetBoolProperty(
            options,
            BuildProperties.AvaloniaNameGeneratorAttachDevTools,
            true);
    }

    public bool AvaloniaNameGeneratorIsEnabled { get; }
    
    public Behavior AvaloniaNameGeneratorBehavior { get; }

    public NamedFieldModifier AvaloniaNameGeneratorClassFieldModifier { get; }

    public ViewFileNamingStrategy AvaloniaNameGeneratorViewFileNamingStrategy { get; }

    public IGlobPattern AvaloniaNameGeneratorFilterByPath { get; }

    public IGlobPattern AvaloniaNameGeneratorFilterByNamespace { get; }

    public bool AvaloniaNameGeneratorAttachDevTools { get; }

    private static string[] GetStringArrayProperty(AnalyzerConfigOptions options, BuildProperties name, string defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue);
        return value.Contains(";") ? value.Split(';') : [value];
    }

    private static TEnum GetEnumProperty<TEnum>(AnalyzerConfigOptions options, BuildProperties name, TEnum defaultValue) where TEnum : struct
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return Enum.TryParse(value, true, out TEnum behavior) ? behavior : defaultValue;
    }

    private static bool GetBoolProperty(AnalyzerConfigOptions options, BuildProperties name, bool defaultValue)
    {
        var key = name.ToString();
        var value = options.GetMsBuildProperty(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
