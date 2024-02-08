using System;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.NameGenerator;
using Microsoft.CodeAnalysis;

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

internal class GeneratorOptions
{
    private readonly GeneratorExecutionContext _context;

    public GeneratorOptions(GeneratorExecutionContext context) => _context = context;

    public bool AvaloniaNameGeneratorIsEnabled => GetBoolProperty(
        BuildProperties.AvaloniaNameGeneratorIsEnabled,
        true);
    
    public Behavior AvaloniaNameGeneratorBehavior => GetEnumProperty(
        BuildProperties.AvaloniaNameGeneratorBehavior,
        Behavior.InitializeComponent);

    public NamedFieldModifier AvaloniaNameGeneratorClassFieldModifier => GetEnumProperty(
        BuildProperties.AvaloniaNameGeneratorDefaultFieldModifier,
        NamedFieldModifier.Internal);

    public ViewFileNamingStrategy AvaloniaNameGeneratorViewFileNamingStrategy => GetEnumProperty(
        BuildProperties.AvaloniaNameGeneratorViewFileNamingStrategy,
        ViewFileNamingStrategy.NamespaceAndClassName);

    public string[] AvaloniaNameGeneratorFilterByPath => GetStringArrayProperty(
        BuildProperties.AvaloniaNameGeneratorFilterByPath,
        "*");

    public string[] AvaloniaNameGeneratorFilterByNamespace => GetStringArrayProperty(
        BuildProperties.AvaloniaNameGeneratorFilterByNamespace,
        "*");

    public bool AvaloniaNameGeneratorAttachDevTools => GetBoolProperty(
        BuildProperties.AvaloniaNameGeneratorAttachDevTools,
        true);

    private string[] GetStringArrayProperty(BuildProperties name, string defaultValue)
    {
        var key = name.ToString();
        var value = _context.GetMsBuildProperty(key, defaultValue);
        return value.Contains(";") ? value.Split(';') : new[] {value};
    }

    private TEnum GetEnumProperty<TEnum>(BuildProperties name, TEnum defaultValue) where TEnum : struct
    {
        var key = name.ToString();
        var value = _context.GetMsBuildProperty(key, defaultValue.ToString());
        return Enum.TryParse(value, true, out TEnum behavior) ? behavior : defaultValue;
    }
    
    private bool GetBoolProperty(BuildProperties name, bool defaultValue)
    {
        var key = name.ToString();
        var value = _context.GetMsBuildProperty(key, defaultValue.ToString());
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }
}
