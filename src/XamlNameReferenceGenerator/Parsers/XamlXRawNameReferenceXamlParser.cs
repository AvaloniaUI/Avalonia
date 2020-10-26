using System.Collections.Generic;
using XamlX;
using XamlX.Ast;
using XamlX.Parsers;

namespace XamlNameReferenceGenerator.Parsers
{
    public class XamlXRawNameReferenceXamlParser : INameReferenceXamlParser
    {
        public List<(string TypeName, string Name)> GetNamedControls(string xaml)
        {
            var parsed = XDocumentXamlParser.Parse(xaml, new Dictionary<string, string>
            {
                {XamlNamespaces.Blend2008, XamlNamespaces.Blend2008}
            });

            var visitor = new XamlAstCollector();
            parsed.Root.Visit(visitor);
            parsed.Root.VisitChildren(visitor);
            return visitor.Controls;
        }

        private class XamlAstCollector : IXamlAstVisitor
        {
            public List<(string TypeName, string Name)> Controls { get; } = new List<(string TypeName, string Name)>();

            public IXamlAstNode Visit(IXamlAstNode node)
            {
                if (node is XamlAstObjectNode element && element.Type is XamlAstXmlTypeReference type)
                {
                    foreach (var child in element.Children)
                    {
                        var nameValue = ResolveNameDirectiveOrDefault(child);
                        if (nameValue == null) continue;

                        var typeNamePair = (type.Name, nameValue);
                        if (!Controls.Contains(typeNamePair))
                            Controls.Add(typeNamePair);
                    }
                }

                return node;
            }

            public void Push(IXamlAstNode node) { }

            public void Pop() { }

            private static string ResolveNameDirectiveOrDefault(IXamlAstNode node) =>
                node switch
                {
                    XamlAstXamlPropertyValueNode propertyValueNode when
                        propertyValueNode.Property is XamlAstNamePropertyReference reference &&
                        reference.Name == "Name" &&
                        propertyValueNode.Values.Count > 0 &&
                        propertyValueNode.Values[0] is XamlAstTextNode nameNode => nameNode.Text,

                    XamlAstXmlDirective xmlDirective when
                        xmlDirective.Name == "Name" &&
                        xmlDirective.Values.Count > 0 &&
                        xmlDirective.Values[0] is XamlAstTextNode xNameNode => xNameNode.Text,

                    _ => null
                };
        }
    }
}