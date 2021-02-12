using System;
using Microsoft.CodeAnalysis;

namespace Avalonia.NameGenerator
{
    public enum BuildProperties
    {
        AvaloniaNameGeneratorBehavior = 0
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

                if (!Enum.TryParse(propertyValue, out Behavior behavior))
                    return Behavior.OnlyProperties;
                return behavior;
            }
        }
    }
}