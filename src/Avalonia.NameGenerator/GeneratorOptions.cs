using System;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator;

public enum BuildProperties
{
    AvaloniaNameGeneratorBehavior = 0,
    AvaloniaNameGeneratorDefaultFieldModifier = 1,
    AvaloniaNameGeneratorFilterByPath = 2,
    AvaloniaNameGeneratorFilterByNamespace = 3,
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

public class GeneratorOptions
{
    private readonly GeneratorExecutionContext _context;

    public GeneratorOptions(GeneratorExecutionContext context) => _context = context;

    public Behavior AvaloniaNameGeneratorBehavior
    {
        get
        {
            const Behavior defaultBehavior = Behavior.InitializeComponent;
            var propertyValue = _context
                .GetMsBuildProperty(
                    nameof(BuildProperties.AvaloniaNameGeneratorBehavior),
                    defaultBehavior.ToString());

            if (!Enum.TryParse(propertyValue, true, out Behavior behavior))
                return defaultBehavior;
            return behavior;
        }
    }

    public DefaultFieldModifier AvaloniaNameGeneratorDefaultFieldModifier
    {
        get
        {
            const DefaultFieldModifier defaultFieldModifier = DefaultFieldModifier.Internal;
            var propertyValue = _context
                .GetMsBuildProperty(
                    nameof(BuildProperties.AvaloniaNameGeneratorDefaultFieldModifier),
                    defaultFieldModifier.ToString());

            if (!Enum.TryParse(propertyValue, true, out DefaultFieldModifier modifier))
                return defaultFieldModifier;
            return modifier;
        }
    }

    public string[] AvaloniaNameGeneratorFilterByPath
    {
        get
        {
            var propertyValue = _context.GetMsBuildProperty(
                nameof(BuildProperties.AvaloniaNameGeneratorFilterByPath),
                "*");

            if (propertyValue.Contains(";"))
                return propertyValue.Split(';');
            return new[] {propertyValue};
        }
    }

    public string[] AvaloniaNameGeneratorFilterByNamespace
    {
        get
        {
            var propertyValue = _context.GetMsBuildProperty(
                nameof(BuildProperties.AvaloniaNameGeneratorFilterByNamespace),
                "*");

            if (propertyValue.Contains(";"))
                return propertyValue.Split(';');
            return new[] {propertyValue};
        }
    }
}