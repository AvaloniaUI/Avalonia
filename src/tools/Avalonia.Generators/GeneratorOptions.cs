using System;
using Avalonia.Generators.Common.Domain;
using Avalonia.Generators.NameGenerator;
using Microsoft.CodeAnalysis;

namespace Avalonia.Generators;

internal enum BuildProperties
{
    AvaloniaNameGeneratorBehavior = 0,
    AvaloniaNameGeneratorDefaultFieldModifier = 1,
    AvaloniaNameGeneratorFilterByPath = 2,
    AvaloniaNameGeneratorFilterByNamespace = 3,
    AvaloniaNameGeneratorViewFileNamingStrategy = 4,
}

internal class GeneratorOptions
{
    private readonly GeneratorExecutionContext _context;

    public GeneratorOptions(GeneratorExecutionContext context) => _context = context;

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
}
