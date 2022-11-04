using System;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator;

public enum BuildProperties
{
    AvaloniaNameGeneratorBehavior = 0,
    AvaloniaNameGeneratorDefaultFieldModifier = 1,
    AvaloniaNameGeneratorFilterByPath = 2,
    AvaloniaNameGeneratorFilterByNamespace = 3,
    AvaloniaNameGeneratorViewFileNamingStrategy = 4,
}

public enum DefaultFieldModifier
{
    Public = 0,
    Private = 1,
    Internal = 2,
    Protected = 3,
}

public enum Behavior
{
    OnlyProperties = 0,
    InitializeComponent = 1,
}

public enum ViewFileNamingStrategy
{
    ClassName = 0,
    NamespaceAndClassName = 1,
}

public class GeneratorOptions
{
    private readonly GeneratorExecutionContext _context;

    public GeneratorOptions(GeneratorExecutionContext context) => _context = context;

    public Behavior AvaloniaNameGeneratorBehavior => GetEnumProperty(
        BuildProperties.AvaloniaNameGeneratorBehavior,
        Behavior.InitializeComponent);

    public DefaultFieldModifier AvaloniaNameGeneratorDefaultFieldModifier => GetEnumProperty(
        BuildProperties.AvaloniaNameGeneratorDefaultFieldModifier,
        DefaultFieldModifier.Internal);

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