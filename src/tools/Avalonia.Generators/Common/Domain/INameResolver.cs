using System.Collections.Immutable;
using System.Threading;
using XamlX.Ast;
using XamlX.TypeSystem;

namespace Avalonia.Generators.Common.Domain;

internal enum NamedFieldModifier
{
    Public = 0,
    Private = 1,
    Internal = 2,
    Protected = 3,
}

internal interface INameResolver
{
    EquatableList<ResolvedXmlName> ResolveXmlNames(XamlDocument xaml, CancellationToken cancellationToken);
    ResolvedName ResolveName(IXamlType xamlType, string name, string? fieldModifier);
}

internal record XamlXmlType(string Name, string? XmlNamespace, EquatableList<XamlXmlType> GenericArguments);

internal record ResolvedXmlName(XamlXmlType XmlType, string Name, string? FieldModifier);
internal record ResolvedName(string TypeName, string Name, string? FieldModifier);
