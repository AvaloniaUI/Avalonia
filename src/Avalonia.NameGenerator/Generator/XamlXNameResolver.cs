using System.Collections.Generic;
using System.Linq;
using Avalonia.NameGenerator.Compiler;
using Avalonia.NameGenerator.Domain;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;

namespace Avalonia.NameGenerator.Generator
{
    internal class XamlXNameResolver : INameResolver, IXamlAstVisitor
    {
        private readonly List<ResolvedName> _items = new();
        private readonly MiniCompiler _compiler;

        public XamlXNameResolver(MiniCompiler compiler) => _compiler = compiler;

        public IReadOnlyList<ResolvedName> ResolveNames(string xaml)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            _compiler.Transform(parsed);
            parsed.Root.Visit(this);
            parsed.Root.VisitChildren(this);
            return _items;
        }

        IXamlAstNode IXamlAstVisitor.Visit(IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode objectNode)
                return node;

            var clrType = objectNode.Type.GetClrType();
            var isAvaloniaControl = clrType
                .Interfaces
                .Any(abstraction => abstraction.IsInterface &&
                                    abstraction.FullName == "Avalonia.Controls.IControl");

            if (!isAvaloniaControl)
                return node;

            foreach (var child in objectNode.Children)
            {
                if (child is XamlAstXamlPropertyValueNode propertyValueNode &&
                    propertyValueNode.Property is XamlAstNamePropertyReference namedProperty &&
                    namedProperty.Name == "Name" &&
                    propertyValueNode.Values.Count > 0 &&
                    propertyValueNode.Values[0] is XamlAstTextNode text)
                {
                    var fieldModifier = TryGetFieldModifier(objectNode);
                    var typeName = $@"{clrType.Namespace}.{clrType.Name}";
                    var resolvedName = new ResolvedName(typeName, text.Text, fieldModifier);
                    if (_items.Contains(resolvedName))
                        continue;
                    _items.Add(resolvedName);
                }
            }

            return node;
        }

        void IXamlAstVisitor.Push(IXamlAstNode node) { }

        void IXamlAstVisitor.Pop() { }

        private static string TryGetFieldModifier(XamlAstObjectNode objectNode)
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
                _ => "internal"
            };
        }
    }
}