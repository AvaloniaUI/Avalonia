using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Avalonia.Generators.Common.Domain;
using XamlX;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common;

internal class XamlXNameResolver
    : INameResolver, IXamlAstVisitor
{
    private readonly Dictionary<string, ResolvedXmlName> _items = new();
    private CancellationToken _cancellationToken;

    public EquatableList<ResolvedXmlName> ResolveXmlNames(XamlDocument xaml, CancellationToken cancellationToken)
    {
        _items.Clear();
        try
        {
            _cancellationToken = cancellationToken;
            xaml.Root.Visit(this);
            xaml.Root.VisitChildren(this);
        }
        finally
        {
            _cancellationToken = CancellationToken.None;
        }

        return new EquatableList<ResolvedXmlName>(_items.Values.ToArray());
    }

    public ResolvedName ResolveName(IXamlType clrType, string name, string? fieldModifier)
    {
        var typeName = $"{clrType.Namespace}.{clrType.Name}";
        var typeAgs = clrType.GenericArguments.Select(arg => arg.FullName).ToImmutableList();
        var genericTypeName = typeAgs.Count == 0
            ? $"global::{typeName}"
            : $"global::{typeName}<{string.Join(", ", typeAgs.Select(arg => $"global::{arg}"))}>";
        return new ResolvedName(genericTypeName, name, fieldModifier);
    }

    IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (node is not XamlAstObjectNode objectNode)
            return node;

        var xamlType = (XamlAstXmlTypeReference)objectNode.Type;

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
                var resolvedName = new ResolvedXmlName(ConvertType(xamlType), text.Text, fieldModifier);
                if (_items.ContainsKey(text.Text))
                    continue;
                _items.Add(text.Text, resolvedName);
            }
        }

        return node;

        static XamlXmlType ConvertType(XamlAstXmlTypeReference type) => new(type.Name, type.XmlNamespace,
            new EquatableList<XamlXmlType>(type.GenericArguments.Select(ConvertType).ToArray()));
    }

    void IXamlAstVisitor.Push(IXamlAstNode node) { }

    void IXamlAstVisitor.Pop() { }

    private string? TryGetFieldModifier(XamlAstObjectNode objectNode)
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
            _ => null
        };
    }

    private static bool IsAttachedProperty(XamlAstNamePropertyReference namedProperty)
    {
        return !namedProperty.DeclaringType.Equals(namedProperty.TargetType);
    }
}
