using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Avalonia.Generators.Common.Domain;
using XamlX;
using XamlX.Ast;

namespace Avalonia.Generators.Common;

internal class XamlXNameResolver : INameResolver, IXamlAstVisitor
{
    private readonly List<ResolvedName> _items = new();
    private readonly string _defaultFieldModifier;

    public XamlXNameResolver(NamedFieldModifier namedFieldModifier = NamedFieldModifier.Internal)
    {
        _defaultFieldModifier = namedFieldModifier.ToString().ToLowerInvariant();
    }

    public IReadOnlyList<ResolvedName> ResolveNames(XamlDocument xaml)
    {
        _items.Clear();
        xaml.Root.Visit(this);
        xaml.Root.VisitChildren(this);
        return _items;
    }

    IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
    {
        if (node is not XamlAstObjectNode objectNode)
            return node;

        var clrType = objectNode.Type.GetClrType();
        if (!clrType.IsAvaloniaStyledElement())
            return node;

        foreach (var child in objectNode.Children)
        {
            if (child is XamlAstXamlPropertyValueNode propertyValueNode &&
                propertyValueNode.Property is XamlAstNamePropertyReference namedProperty &&
                !IsAttachedProperty(namedProperty) &&
                namedProperty.Name == "Name" &&
                propertyValueNode.Values.Count > 0 &&
                propertyValueNode.Values[0] is XamlAstTextNode text)
            {
                var fieldModifier = TryGetFieldModifier(objectNode);
                var typeName = $@"{clrType.Namespace}.{clrType.Name}";
                var typeAgs = clrType.GenericArguments.Select(arg => arg.FullName).ToImmutableList();
                var genericTypeName = typeAgs.Count == 0
                    ? $"global::{typeName}"
                    : $@"global::{typeName}<{string.Join(", ", typeAgs.Select(arg => $"global::{arg}"))}>";

                var resolvedName = new ResolvedName(genericTypeName, text.Text, fieldModifier);
                if (_items.Contains(resolvedName))
                    continue;
                _items.Add(resolvedName);
            }
        }

        return node;
    }

    void IXamlAstVisitor.Push(IXamlAstNode node) { }

    void IXamlAstVisitor.Pop() { }

    private string TryGetFieldModifier(XamlAstObjectNode objectNode)
    {
        // We follow Xamarin.Forms API behavior in terms of x:FieldModifier here:
        // https://docs.microsoft.com/en-us/xamarin/xamarin-forms/xaml/field-modifiers
        // However, by default we use 'internal' field modifier here for generated
        // x:Name references for historical purposes and WPF compatibility.
        //
        var fieldModifierType = objectNode
            .Children
            .OfType<XamlAstXmlDirective>()
            .Where(dir => dir.Name == "FieldModifier" && dir.Namespace == XamlNamespaces.Xaml2006)
            .Select(dir => dir.Values[0])
            .OfType<XamlAstTextNode>()
            .Select(txt => txt.Text)
            .FirstOrDefault();

        return fieldModifierType?.ToLowerInvariant() switch
        {
            "private" => "private",
            "public" => "public",
            "protected" => "protected",
            "internal" => "internal",
            "notpublic" => "internal",
            _ => _defaultFieldModifier
        };
    }

    private static bool IsAttachedProperty(XamlAstNamePropertyReference namedProperty)
    {
        return !namedProperty.DeclaringType.Equals(namedProperty.TargetType);
    }
}
