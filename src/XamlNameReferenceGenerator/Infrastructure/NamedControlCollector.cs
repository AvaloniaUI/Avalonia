using System.Collections.Generic;
using XamlX.Ast;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal sealed class NamedControlCollector : IXamlAstVisitor
    {
        private readonly List<(string TypeName, string Name)> _items = new List<(string TypeName, string Name)>();

        public IReadOnlyList<(string TypeName, string Name)> Controls => _items;

        public IXamlAstNode Visit(IXamlAstNode node)
        {
            if (node is XamlAstConstructableObjectNode constructableObjectNode)
            {
                foreach (var child in constructableObjectNode.Children)
                {
                    if (child is XamlAstXamlPropertyValueNode propertyValueNode &&
                        propertyValueNode.Property is XamlAstClrProperty clrProperty &&
                        clrProperty.Name == "Name" &&
                        propertyValueNode.Values.Count > 0 &&
                        propertyValueNode.Values[0] is XamlAstTextNode text)
                    {
                        var clrType = constructableObjectNode.Type.GetClrType();
                        var typeNamePair = ($@"{clrType.Namespace}.{clrType.Name}", text.Text);

                        if (!_items.Contains(typeNamePair))
                        {
                            _items.Add(typeNamePair);
                        }
                    }
                }

                return node;
            }

            return node;
        }

        public void Push(IXamlAstNode node) { }

        public void Pop() { }
    }
}