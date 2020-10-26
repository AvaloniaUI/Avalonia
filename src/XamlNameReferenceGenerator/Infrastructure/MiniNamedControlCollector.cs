using System.Collections.Generic;
using XamlX.Ast;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal sealed class MiniNamedControlCollector : IXamlAstVisitor
    {
        private readonly List<(string TypeName, string Name)> _items = new List<(string TypeName, string Name)>();

        public IReadOnlyList<(string TypeName, string Name)> Controls => _items;

        public IXamlAstNode Visit(IXamlAstNode node)
        {
            if (!(node is XamlAstConstructableObjectNode constructableObjectNode))
                return node;

            foreach (var child in constructableObjectNode.Children)
            {
                var nameValue = ResolveNameDirectiveOrDefault(child);
                if (nameValue == null) continue;
                    
                var clrType = constructableObjectNode.Type.GetClrType();
                var typeNamePair = ($@"{clrType.Namespace}.{clrType.Name}", nameValue);
                if (!_items.Contains(typeNamePair))
                {
                    _items.Add(typeNamePair);
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
                    propertyValueNode.Property is XamlAstClrProperty reference &&
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