#nullable enable

using System.Collections.Generic;
using System.Linq;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Visitors;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;

internal class AvaloniaXamlIlControlTemplatePartsChecker : IXamlAstTransformer
{
    public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
    {
        if (!(node is AvaloniaXamlIlTargetTypeMetadataNode on
              && on.ScopeType == AvaloniaXamlIlTargetTypeMetadataNode.ScopeTypes.ControlTemplate
              // Styles with template selector will also return ScopeTypes.ControlTemplate, so we need to double check.
              && on.Value.Type.GetClrType() == context.GetAvaloniaTypes().ControlTemplate))
            return node;

        var targetType = on.TargetType.GetClrType();
        var templateParts = ResolveTemplateParts(targetType);

        if (templateParts.Count == 0)
            return node;

        var visitor = new NameScopeRegistrationVisitor();
        node.VisitChildren(visitor);

        foreach (var pair in templateParts)
        {
            var name = pair.Key;
            var (expectedType, isRequired) = pair.Value;

            if (!visitor.TryGetValue(name, out var res))
            {
                if (isRequired)
                {
                    context.ReportDiagnostic(new XamlDiagnostic(
                        AvaloniaXamlDiagnosticCodes.RequiredTemplatePartMissing,
                        XamlDiagnosticSeverity.Error,
                        $"Required template part with x:Name '{name}' must be defined on '{targetType.Name}' ControlTemplate.",
                        node));
                }
                else
                {
                    context.ReportDiagnostic(new XamlDiagnostic(
                        AvaloniaXamlDiagnosticCodes.OptionalTemplatePartMissing,
                        XamlDiagnosticSeverity.None,
                        $"Optional template part with x:Name '{name}' can be defined on '{targetType.Name}' ControlTemplate.",
                        node));
                }

                continue;
            }

            if (expectedType is not null
                && !expectedType.IsAssignableFrom(res.type))
            {
                context.ReportDiagnostic(new XamlDiagnostic(
                    AvaloniaXamlDiagnosticCodes.TemplatePartWrongType,
                    XamlDiagnosticSeverity.Error, 
                    $"Template part '{name}' is expected to be assignable to '{expectedType.Name}', but actual type is {res.type.Name}.",
                    res.line));
            }
        }

        return node;
    }

    private static Dictionary<string, (IXamlType? type, bool isRequired)> ResolveTemplateParts(IXamlType targetType)
    {
        var dictionary = new Dictionary<string, (IXamlType? type, bool isRequired)>();
        // Custom Attributes go in order from current type to base type. It should be possible to override parent template parts.
        foreach (var attr in targetType.GetAllCustomAttributes())
        {
            if (attr.Type.Name == "TemplatePartAttribute")
            {
                if (!attr.Properties.TryGetValue("Name", out var nameObj))
                {
                    nameObj = attr.Parameters.FirstOrDefault();
                }

                if (!attr.Properties.TryGetValue("Type", out var typeObj))
                {
                    typeObj = attr.Parameters.Skip(1).FirstOrDefault();
                }

                attr.Properties.TryGetValue("IsRequired", out var isRequiredObj);

                if (nameObj is string { Length :> 0 } name
                    && !dictionary.ContainsKey(name))
                {
                    dictionary.Add(name, (typeObj as IXamlType, isRequiredObj as bool? == true));
                }
            }
        }

        return dictionary;
    }
}
