using System.Collections.Generic;
using System.Linq;
using XamlX.Ast;

namespace XamlNameReferenceGenerator.Infrastructure
{
    internal sealed class NameReceiver : IXamlAstVisitor
    {
        private readonly List<(string TypeName, string Name)> _items = new List<(string TypeName, string Name)>();

        public IReadOnlyList<(string TypeName, string Name)> Controls => _items;

        public IXamlAstNode Visit(IXamlAstNode node)
        {
            if (node is XamlAstConstructableObjectNode constructableObjectNode)
            {
                var clrType = constructableObjectNode.Type.GetClrType();
                var isAvaloniaControl = clrType
                    .Interfaces
                    .Any(abstraction => abstraction.IsInterface &&
                                        abstraction.FullName == "Avalonia.Controls.IControl");

                if (!isAvaloniaControl)
                {
                    return node;
                }

                foreach (var child in constructableObjectNode.Children)
                {
                    if (child is XamlAstXamlPropertyValueNode propertyValueNode &&
                        propertyValueNode.Property is XamlAstNamePropertyReference namedProperty &&
                        namedProperty.Name == "Name" &&
                        propertyValueNode.Values.Count > 0 &&
                        propertyValueNode.Values[0] is XamlAstTextNode text)
                    {
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