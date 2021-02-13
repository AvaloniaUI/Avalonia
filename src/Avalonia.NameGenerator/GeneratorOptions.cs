using System;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator
{
    public enum BuildProperties
    {
        AvaloniaNameGeneratorBehavior = 0,
        AvaloniaNameGeneratorDefaultFieldModifier = 1,
        AvaloniaNameGeneratorFilterByPath = 2,
        AvaloniaNameGeneratorFilterByNamespace = 2,
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
                var propertyValue = _context
                    .GetMSBuildProperty(
                        nameof(BuildProperties.AvaloniaNameGeneratorBehavior),
                        nameof(Behavior.OnlyProperties));

                if (!Enum.TryParse(propertyValue, true, out Behavior behavior))
                    return Behavior.OnlyProperties;
                return behavior;
            }
        }

        public DefaultFieldModifier AvaloniaNameGeneratorDefaultFieldModifier
        {
            get
            {
                var propertyValue = _context
                    .GetMSBuildProperty(
                        nameof(BuildProperties.AvaloniaNameGeneratorDefaultFieldModifier),
                        nameof(DefaultFieldModifier.Internal));

                if (!Enum.TryParse(propertyValue, true, out DefaultFieldModifier modifier))
                    return DefaultFieldModifier.Internal;
                return modifier;
            }
        }

        public string[] AvaloniaNameGeneratorFilterByPath
        {
            get
            {
                var propertyValue = _context.GetMSBuildProperty(
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
                var propertyValue = _context.GetMSBuildProperty(
                    nameof(BuildProperties.AvaloniaNameGeneratorFilterByNamespace),
                    "*");

                if (propertyValue.Contains(";"))
                    return propertyValue.Split(';');
                return new[] {propertyValue};
            }
        }
    }
}