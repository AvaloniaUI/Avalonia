using Microsoft.CodeAnalysis;

namespace Avalonia.Analyzers.GeneratedProperties;

internal sealed class GeneratedPropertyAttributeArgs
{
    public ITypeSymbol? AddOwnerFrom { get; private set; }
    public TypedConstant? DefaultValue { get; private set; }
    public TypedConstant? UnsetValue { get; private set; }
    /// Raw enum value; null when the named argument is absent.
    public int? DefaultBindingMode { get; private set; }
    public bool Inherits { get; private set; }
    public bool EnableDataValidation { get; private set; }
    public string? ChangedMethodName { get; private set; }
    public string? ValidateMethodName { get; private set; }
    public string? CoerceMethodName { get; private set; }

    public static GeneratedPropertyAttributeArgs Read(AttributeData attribute)
    {
        var args = new GeneratedPropertyAttributeArgs();
        foreach (var named in attribute.NamedArguments)
        {
            var value = named.Value;
            switch (named.Key)
            {
                case nameof(AddOwnerFrom):
                    args.AddOwnerFrom = value.Value as ITypeSymbol;
                    break;
                case nameof(DefaultValue):
                    args.DefaultValue = value;
                    break;
                case nameof(UnsetValue):
                    args.UnsetValue = value;
                    break;
                case nameof(DefaultBindingMode):
                    args.DefaultBindingMode = value.Value is int i ? i : null;
                    break;
                case nameof(Inherits):
                    args.Inherits = value.Value is true;
                    break;
                case nameof(EnableDataValidation):
                    args.EnableDataValidation = value.Value is true;
                    break;
                case nameof(ChangedMethodName):
                    args.ChangedMethodName = value.Value as string;
                    break;
                case nameof(ValidateMethodName):
                    args.ValidateMethodName = value.Value as string;
                    break;
                case nameof(CoerceMethodName):
                    args.CoerceMethodName = value.Value as string;
                    break;
            }
        }

        return args;
    }
}
